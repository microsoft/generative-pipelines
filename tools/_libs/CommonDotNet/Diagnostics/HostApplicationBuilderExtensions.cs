// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using CommonDotNet.ServiceDiscovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CommonDotNet.Diagnostics;

public static partial class HostApplicationBuilderExtensions
{
    public static string GetAppName(this IHostApplicationBuilder builder)
    {
        return Environment.GetEnvironmentVariable(ToolRegistry.ToolNameEnvVar)
               ?? Assembly.GetExecutingAssembly().GetName().Name?.Replace(".", "")
               ?? "unknown-tool";
    }

    public static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder, string appName)
    {
        builder.Services.AddLogging(x => { x.AddConsole(); });
        builder.AddOpenTelemetry(appName);
        return builder;
    }

    public static IHostApplicationBuilder AddOpenTelemetry(this IHostApplicationBuilder builder, string appName)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(appName)
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(t =>
                        // Don't trace requests to the health endpoint to avoid filling the dashboard with noise
                        t.Filter = httpContext => !(httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
                                                    || httpContext.Request.Path.StartsWithSegments("/alive", StringComparison.OrdinalIgnoreCase))
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
