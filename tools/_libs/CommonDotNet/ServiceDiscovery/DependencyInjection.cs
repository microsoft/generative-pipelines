// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommonDotNet.ServiceDiscovery;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRedisToolsRegistry(
        this IHostApplicationBuilder builder, string connectionName = "redis-storage")
    {
        // Check if the registry integration is enabled
        if (builder.Configuration.GetValue<bool>("GenerativePipelines:ToolsRegistryEnabled"))
        {
            builder.AddRedisClient(
                connectionName: connectionName,
                configureSettings: x =>
                {
                    x.DisableTracing = false;
                },
                configureOptions: x =>
                {
                    x.LibraryName = Telemetry.HttpUserAgent;
                });
            builder.Services.AddSingleton<ToolRegistry, ToolRegistry>();
        }
        else
        {
            builder.Services.AddSingleton<ToolRegistry>(sp => new ToolRegistry(null!, null));
        }

        return builder;
    }
}
