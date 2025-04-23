// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using CommonDotNet.Models;
using CommonDotNet.OpenApi;
using CommonDotNet.ServiceDiscovery;
using TextGenerator.Config;
using TextGenerator.Functions;

namespace TextGenerator;

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
        builder.Services.AddScoped<GenerateTextFunction>();
        builder.Services.AddScoped<GenerateChatReplyFunction>();

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Dependencies
        var appConfig = app.Services.GetService<AppConfig>()!;

        // Register endpoints
        var registry = app.Services.GetService<ToolRegistry>();
        if (registry == null) { app.Logger.LogWarning("Tool registry not available, skipping functions registration"); }

        // Endpoints / Functions
        const string GenerateTextFunctionName = "generate";
        // const string GenerateChatReplyFunctionName = "generate-reply";
        const string ListModelsFunctionName = "list-models";

        registry?.RegisterPostFunction($"/{GenerateTextFunctionName}", "Generate text for a given prompt");
        app.MapPost($"/{GenerateTextFunctionName}", async Task<IResult> (
                GenerateTextFunction function,
                GenerateTextRequest req,
                CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
            .Produces<GenerateTextResponse>(StatusCodes.Status200OK)
            .WithName(GenerateTextFunctionName)
            .WithDisplayName("Generate text")
            .WithDescription("Generate text for a given prompt")
            .WithSummary("Generate text for a given prompt");

        // registry?.RegisterPostFunction($"/{GenerateChatReplyFunctionName}", "Generate reply for a given chat");
        // app.MapPost($"/{GenerateChatReplyFunctionName}", async Task<IResult> (
        //         GenerateChatReplyFunction function,
        //         GenerateChatReplyRequest req,
        //         CancellationToken cancellationToken) => await function.InvokeAsync(req, cancellationToken).ConfigureAwait(false))
        //     .Produces<GenerateChatReplyResponse>(StatusCodes.Status200OK)
        //     .WithName(GenerateChatReplyFunctionName)
        //     .WithDisplayName("Generate chat reply")
        //     .WithDescription("Generate reply for a given chat history")
        //     .WithSummary("Generate reply for a given chat history");

        registry?.RegisterPostFunction($"/{ListModelsFunctionName}", "List the models available to generate text and replies");
        app.MapPost($"/{ListModelsFunctionName}", IResult () =>
            {
                Dictionary<string, ModelInfo> list = appConfig.GetModelsInfo();
                return Results.Ok(list);
            })
            .Produces<Dictionary<string, Dictionary<string, object>>>(StatusCodes.Status200OK)
            .WithName(ListModelsFunctionName)
            .WithDisplayName("List text models")
            .WithDescription("Get the list of available LLM models")
            .WithSummary("Get the list of available LLM models");

        app.Run();
    }
}
