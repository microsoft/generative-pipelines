// Copyright (c) Microsoft. All rights reserved.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orchestrator.Orchestration;

namespace Orchestrator.Diagnostics;

internal static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddLogging(x => { x.AddConsole(); });
        builder.AddOpenTelemetry();
        return builder;
    }

    public static IHostApplicationBuilder AddOpenTelemetry(this IHostApplicationBuilder builder)
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
                    .AddSource(SynchronousOrchestrator.ActivitySourceName)
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
