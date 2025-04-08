// Copyright (c) Microsoft. All rights reserved.

using Chunker.Functions;
using Chunker.Models;
using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using CommonDotNet.OpenApi;
using CommonDotNet.ServiceDiscovery;

namespace Chunker;

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
        builder.Services.AddScoped<ChunkFunction>();

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Orchestrator's tools registry
        var registry = app.Services.GetService<ToolRegistry>();
        if (registry == null) { app.Logger.LogWarning("Tool registry not available, skipping functions registration"); }

        // Endpoints / Functions
        const string ChunkFunctionName = "chunk";

        registry?.RegisterPostFunction($"/{ChunkFunctionName}", "Chunk a given text into smaller parts");
        app.MapPost($"/{ChunkFunctionName}", IResult (ChunkFunction function, ChunkRequest req) => function.Invoke(req))
            .Produces<ChunkResponse>(StatusCodes.Status200OK)
            .WithName(ChunkFunctionName)
            .WithDisplayName("Content chunker")
            .WithDescription("Chunk a given text into smaller parts")
            .WithSummary("Chunk a given text into smaller parts");

        app.Run();
    }
}
