// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Orchestrator.Config;
using Orchestrator.Diagnostics;
using Orchestrator.Http;
using Orchestrator.Models;
using Orchestrator.Orchestration;
using Orchestrator.ServiceDiscovery;
using StackExchange.Redis;

namespace Orchestrator;

internal static class Program
{
    private const string BlobStorageName = "blobstorage";
    private const string RedisStorageName = "redisstorage";
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };

    public static void Main(string[] args)
    {
        // App setup
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder
            .AddLogging()
            .AddOrchestrationWorkspace(blobStorageName: BlobStorageName)
            .AddRedisClient(connectionName: RedisStorageName);
        // builder.AddAzureBlobClient("blobstorage", configureClientBuilder: b => b.ConfigureOptions(o => o.Diagnostics.ApplicationId = Telemetry.HttpUserAgent));
        builder.Services
            .ConfigureSerializationOptions()
            .AddOpenApi()
            .AddToolsHttpClients(builder.Configuration);
        // .AddOrchestrationWorkspace(useLocalFileSystem: false);

        builder.Services.AddSingleton(builder.Configuration.GetSection("App").Get<AppConfig>()?.Validate() ?? throw new ApplicationException(nameof(AppConfig) + " not available"));
        builder.Services.AddSingleton<SynchronousOrchestrator>();

        var connectionStrings = builder.Configuration.GetSection("ConnectionStrings").Get<Dictionary<string, string>>();

        // Abb build
        var app = builder.Build();
        app.UseMiddleware<YamlToJsonMiddleware>();
        app.MapOpenApi();

        // UseDefaultFiles must be called before UseStaticFiles to serve the default file
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = (StaticFileResponseContext ctx) =>
            {
                var cacheMaxAgeSecs = app.Environment.IsDevelopment() ? 0 : 10;
                ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cacheMaxAgeSecs}");
            },
        });

        // Dependencies
        var log = app.Logger;
        var appConfig = app.Services.GetService<AppConfig>()!;
        var orchestrator = app.Services.GetService<SynchronousOrchestrator>()!;
        var workspaceConfig = app.Services.GetService<WorkspaceConfig>()!;
        var tools = ToolDiscovery.GetTools(app.Configuration);
        var authFilter = new HttpAuthEndpointFilter(appConfig.Authorization);

        if (workspaceConfig.UseFileSystem)
        {
            log.LogInformation("Starting Orchestrator, workspace on disk: {WorkspaceDir}", Logging.RemovePiiFromPath(workspaceConfig.WorkspaceDir));
        }
        else
        {
            log.LogInformation("Starting Orchestrator, workspace on blob storage: {BlobStorageDetails}", connectionStrings?.GetValueOrDefault(BlobStorageName, "-"));
        }

        // =====================================================================
        // =====================================================================
        // =====================================================================
        app.MapPost("/api/jobs", async Task<IResult> (
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var contentType = httpContext.Request.ContentType ?? string.Empty;
                bool isMultipart = contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase);

                var (workflow, input, error) = isMultipart
                    ? await JobCreationRequestParser.ParseMultipartInputAsync(httpContext, cancellationToken).ConfigureAwait(false)
                    : await JobCreationRequestParser.ParseJsonInputAsync(httpContext, cancellationToken).ConfigureAwait(false);

                if (error != null)
                {
                    log.LogError("An error occurred while parsing the request: {Error}", error);
                    return error;
                }

                if (workflow == null)
                {
                    log.LogCritical("The {ObjectName} object is null but no error was returned", nameof(workflow));
                    return Results.InternalServerError($"Unable to parse request: {nameof(workflow)} is null");
                }

                httpContext.Response.Headers["X-Job-Id"] = workflow.JobId;

                if (input == null)
                {
                    log.LogCritical("Job: {JobId}: The {ObjectName} object is null but no error was returned", workflow.JobId, nameof(input));
                    return Results.InternalServerError($"Unable to parse request: {nameof(input)} is null");
                }

                // TODO: store duration into workflow metadata
                var clock = new Stopwatch();
                clock.Start();

                (object? result, string workflowId, IResult? error) result = await orchestrator.RunWorkflowAsync(
                    input, workflow, cancellationToken).ConfigureAwait(false);

                clock.Stop();

                log.LogInformation("Job {JobId} completed successfully in {Duration} msecs", result.workflowId, clock.ElapsedMilliseconds);
                return result.error ?? Results.Ok(result.result);
            })
            .AddEndpointFilter(authFilter)
            .Produces<OrchestratorStatus>(StatusCodes.Status200OK)
            .Produces<OrchestratorStatus>(StatusCodes.Status401Unauthorized)
            .Produces<OrchestratorStatus>(StatusCodes.Status403Forbidden)
            .WithName("process");

        // =========================================================================================
        app.MapGet("/tools", async Task<IResult> (
                HttpContext ctx,
                IServiceProvider sp,
                HttpClient httpClient,
                CancellationToken cancellationToken) =>
            {
                IConnectionMultiplexer? redisConnectionMultiplexer = null;

                try
                {
                    redisConnectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                }
                catch (InvalidOperationException)
                {
                    // Redis not available
                }

                Dictionary<string, FunctionDescription> functionsInfo = new();
                Dictionary<string, ToolInfo> toolsInfo = new();
                if (redisConnectionMultiplexer != null)
                {
                    var registry = new ToolRegistry(redisConnectionMultiplexer.GetDatabase());

                    // Fetch list of functions from Redis
                    List<FunctionDescription> functions = await registry.GetFunctionsAsync(cancellationToken).ConfigureAwait(false);

                    functionsInfo = functions
                        .OrderBy(x => x.Tool)
                        .ToDictionary(f => f.Id, f => f);

                    // Fetch list tool details from each tool
                    toolsInfo = (await registry.FetchToolsAsync(tools, httpClient, cancellationToken).ConfigureAwait(false))
                        .ToDictionary(x => x.Name, x => x);
                }

                var data = new OrchestratorStatus
                {
                    Tools = toolsInfo,
                    Functions = functionsInfo
                };

                // Set no-cache headers
                ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                ctx.Response.Headers["Pragma"] = "no-cache";
                ctx.Response.Headers["Expires"] = "0";

                return Results.Content(JsonSerializer.Serialize(data, s_jsonSerializerOptions), "application/json; charset=utf-8");
            })
            .AddEndpointFilter(authFilter)
            .Produces<OrchestratorStatus>(StatusCodes.Status200OK)
            .Produces<OrchestratorStatus>(StatusCodes.Status401Unauthorized)
            .Produces<OrchestratorStatus>(StatusCodes.Status403Forbidden)
            .WithName("functions");

        // =========================================================================================
        app.MapGet("/env", IResult () =>
            {
                var data = new OrchestratorStatus
                {
                    WorkspaceDir = Logging.RemovePiiFromPath(workspaceConfig.WorkspaceDir),
                    Environment = new
                    {
                        CurrentDirectory = Logging.RemovePiiFromPath(Environment.CurrentDirectory),
                        Version = Environment.Version,
                        OSVersion = Environment.OSVersion,
                        Is64BitProcess = Environment.Is64BitProcess,
                        Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                        IsPrivilegedProcess = Environment.IsPrivilegedProcess,
                        CommandLine = Logging.RemovePiiFromPath(Environment.CommandLine),
                        ProcessPath = Logging.RemovePiiFromPath(Environment.ProcessPath),
                        CurrentManagedThreadId = Environment.CurrentManagedThreadId,
                        ProcessId = Environment.ProcessId,
                        ProcessorCount = Environment.ProcessorCount,
                        UserInteractive = Environment.UserInteractive,
                        CpuUsage = Environment.CpuUsage,
                    }
                };

                return Results.Content(
                    JsonSerializer.Serialize(data, s_jsonSerializerOptions),
                    "application/json; charset=utf-8");
            })
            .AddEndpointFilter(authFilter)
            .Produces<OrchestratorStatus>(StatusCodes.Status200OK)
            .Produces<OrchestratorStatus>(StatusCodes.Status401Unauthorized)
            .Produces<OrchestratorStatus>(StatusCodes.Status403Forbidden)
            .WithName("index");

        app.Run();
    }
}
