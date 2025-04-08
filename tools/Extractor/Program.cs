// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using CommonDotNet.OpenApi;
using CommonDotNet.ServiceDiscovery;
using Extractor.Functions;
using Extractor.Models;
using Microsoft.KernelMemory.Pipeline;

namespace Extractor;

internal static class Program
{
    public static void Main(string[] args)
    {
        // App setup
        var builder = WebApplication.CreateBuilder(args);
        builder.AddLogging(builder.GetAppName());
        builder.AddRedisToolsRegistry();
        builder.Services.AddOpenApi();
        builder.Services.ConfigureSerializationOptions();
        builder.Services.AddScoped<MimeTypesDetection>();
        builder.Services.AddScoped<Extractor>();
        builder.Services.AddScoped<ExtractFunction>();
        builder.Services.AddScoped<ExtractMultipartFunction>();

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Orchestrator's tools registry
        var registry = app.Services.GetService<ToolRegistry>();
        if (registry == null) { app.Logger.LogWarning("Tool registry not available, skipping functions registration"); }

        // Endpoints / Functions
        const string ExtractFunctionName = "extract";
        const string ExtractMultipartFunctionName = "extract-multipart";

        registry?.RegisterPostFunction($"/{ExtractFunctionName}", "Extract text from a given file uploaded as base64");
        app.MapPost($"/{ExtractFunctionName}", async Task<IResult> (
                ExtractFunction function,
                ExtractRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<ExtractResponse>(StatusCodes.Status200OK)
            .WithName(ExtractFunctionName)
            .WithDisplayName("File content extractor")
            .WithDescription("Extract text from a given file, using JSON payload")
            .WithSummary("Extract text from a given file, using JSON payload");

        registry?.RegisterPostMultipartFunction($"/{ExtractMultipartFunctionName}", "Extract text from a given file uploaded as multipart");
        app.MapPost($"/{ExtractMultipartFunctionName}", async Task<IResult> (
                ExtractMultipartFunction function,
                HttpContext httpContext,
                CancellationToken cancellationToken) => await function.InvokeAsync(httpContext, cancellationToken).ConfigureAwait(false))
            .Produces<ExtractResponse>(StatusCodes.Status200OK)
            .WithName(ExtractMultipartFunctionName)
            .WithDisplayName("File content extractor")
            .WithDescription("Extracts text from a given file, using multipart-form payload")
            .WithSummary("Extracts text from a given file, using multipart-form payload");

        app.Run();
    }
}
