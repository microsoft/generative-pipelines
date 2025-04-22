// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using CommonDotNet.Models;
using CommonDotNet.OpenApi;
using CommonDotNet.ServiceDiscovery;
using EmbeddingGenerator.Config;
using EmbeddingGenerator.Functions;

namespace EmbeddingGenerator;

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
        builder.Services.AddSingleton(builder.Configuration.GetSection("App").Get<AppConfig>().EnsureValid());
        builder.Services.AddScoped<EmbeddingFunction>();
        builder.Services.AddScoped<CustomEmbeddingFunction>();

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Dependencies
        var appConfig = app.Services.GetService<AppConfig>()!;

        // Register endpoints
        var registry = app.Services.GetService<ToolRegistry>();
        if (registry == null) { app.Logger.LogWarning("Tool registry not available, skipping functions registration"); }

        // Endpoints / Functions
        const string VectorizeFunctionName = "vectorize";
        const string VectorizeCustomFunctionName = "vectorize-custom";
        const string ListModelsFunctionName = "list-models";

        registry?.RegisterPostFunction($"/{VectorizeFunctionName}", "Generate embedding for a list of strings");
        app.MapPost($"/{VectorizeFunctionName}", async Task<IResult> (
                EmbeddingFunction function,
                EmbeddingRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<EmbeddingResponse>(StatusCodes.Status200OK)
            .WithName(VectorizeFunctionName)
            .WithDisplayName("Generate embedding(s)")
            .WithDescription("Generate embedding for one or multiple strings")
            .WithSummary("Generate embedding for one or multiple strings");

        registry?.RegisterPostFunction($"/{VectorizeCustomFunctionName}", "Use a non-configured model to generate embedding for a list of strings, passing in all the authentication parameters");
        app.MapPost($"/{VectorizeCustomFunctionName}", async Task<IResult> (
                CustomEmbeddingFunction function,
                CustomEmbeddingRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<EmbeddingResponse>(StatusCodes.Status200OK)
            .WithName(VectorizeCustomFunctionName)
            .WithDisplayName("Generate embedding(s) with custom model")
            .WithDescription("Generate embedding for one or multiple strings using custom endpoint and authentication parameters")
            .WithSummary("Generate embedding for one or multiple strings using custom endpoint and authentication parameters");

        registry?.RegisterPostFunction($"/{ListModelsFunctionName}", "List the models available to generate text embeddings");
        app.MapPost($"/{ListModelsFunctionName}", IResult () =>
            {
                Dictionary<string, ModelInfo> list = appConfig.GetModelsInfo();
                return Results.Ok(list);
            })
            .Produces<Dictionary<string, Dictionary<string, object>>>(StatusCodes.Status200OK)
            .WithName(ListModelsFunctionName)
            .WithDisplayName("List embedding models")
            .WithDescription("Get the list of available embedding providers")
            .WithSummary("Get the list of available embedding providers");

        app.Run();
    }
}
