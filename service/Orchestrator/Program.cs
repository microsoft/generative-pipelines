// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Orchestrator.Diagnostics;
using Orchestrator.Http;
using Orchestrator.Models;
using Orchestrator.Orchestration;
using Orchestrator.ServiceDiscovery;
using StackExchange.Redis;

namespace Orchestrator;

internal static class Program
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };

    public static void Main(string[] args)
    {
        // App setup
        var builder = WebApplication.CreateBuilder(args);
        builder
            .AddLogging()
            .AddRedisClient(connectionName: "redisstorage");
        builder.Services
            .ConfigureSerializationOptions()
            .AddOpenApi()
            .AddToolsHttpClients(builder.Configuration)
            .AddOrchestrationWorkspace();

        builder.Services.AddSingleton(builder.Configuration.GetSection("App").Get<AppConfig>()?.Validate() ?? throw new ApplicationException(nameof(AppConfig) + " not available"));
        builder.Services.AddSingleton(builder.Configuration.GetSection("App:Orchestrator").Get<OrchestratorConfig>()?.Validate() ?? throw new ApplicationException(nameof(OrchestratorConfig) + " not available"));
        builder.Services.AddSingleton<SimpleWorkspace>();
        builder.Services.AddSingleton<SynchronousOrchestrator>();

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
        var orchestratorConfig = app.Services.GetService<OrchestratorConfig>()!;
        var tools = ToolDiscovery.GetTools(app.Configuration);
        var authFilter = new HttpAuthEndpointFilter(appConfig.Authorization);

        log.LogInformation("Starting Orchestrator, workspace: {Workspace},", Logging.RemovePiiFromPath(orchestratorConfig.WorkspaceDir));

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

                (object? result, string workflowId, IResult? error) result = await orchestrator.RunWorkflowAsync(
                    input, workflow, cancellationToken).ConfigureAwait(false);

                log.LogInformation("Job {JobId} completed successfully", result.workflowId);
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
                    WorkspaceDir = Logging.RemovePiiFromPath(orchestratorConfig.WorkspaceDir),
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
