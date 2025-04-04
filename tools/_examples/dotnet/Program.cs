// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Diagnostics;
using CommonDotNet.ServiceDiscovery;
using dotnetTemplate.Models;
using dotnetTemplate.OpenApi;

namespace dotnetTemplate;

internal static class Program
{
    public static void Main(string[] args)
    {
        // App setup
        var builder = WebApplication.CreateBuilder(args);
        builder.AddLogging(builder.GetAppName());
        builder.AddRedisToolsRegistry();
        builder.Services.AddOpenApi();

        // Abb build
        var app = builder.Build();
        app.AddOpenApiDevTools();

        // Dependencies
        var log = app.Logger;

        // Register endpoints
        var registry = app.Services.GetService<ToolRegistry>();
        registry?.RegisterPostFunction("/example", "Sample function");

        // =====================================================================
        // =====================================================================
        // =====================================================================
        app.MapPost("/example", async Task<IResult> (
                    FooRequest request,
                    CancellationToken cancellationToken) =>
                {
                    await Task.Delay(0, cancellationToken).ConfigureAwait(false);
                    return Results.Ok(new FooResponse { Message = request.Message });
                }
            )
            .Produces<FooResponse>(StatusCodes.Status200OK)
            .WithName("foo")
            .WithDisplayName("Sample function")
            .WithDescription("Doesnt' do anything")
            .WithSummary("Sample function that doesn't do anything");

        app.Run();
    }
}
