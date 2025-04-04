// Copyright (c) Microsoft. All rights reserved.

using System.Net.Mime;
using Microsoft.Net.Http.Headers;
using Orchestrator.ServiceDiscovery;
using Orchestrator.Storage;

namespace Orchestrator;

#pragma warning disable CA1724
public static partial class DependencyInjection
{
    public static IServiceCollection AddOrchestrationWorkspace(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        return services;
    }

    public static IServiceCollection AddToolsHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var tools = ToolDiscovery.GetTools(configuration);
        foreach (KeyValuePair<string, string> t in tools)
        {
            services.AddHttpClient(t.Key, client =>
            {
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
                client.BaseAddress = new(t.Value.TrimEnd('/'));
            });
        }

        // Ensure that IHttpClientFactory is registered
        if (tools.Count == 0)
        {
            services.AddHttpClient("localhost", client =>
            {
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
                client.BaseAddress = new("http://localhost");
            });
        }

        return services;
    }
}
