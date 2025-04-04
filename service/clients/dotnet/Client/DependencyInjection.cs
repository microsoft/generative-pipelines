// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.GenerativePipelines;

#pragma warning disable CA1724 // reason
public static partial class DependencyInjection
{
    public static IServiceCollection UseGenerativePipelines(this IServiceCollection services, string baseUrl, string? apiKey = null)
    {
        services.Configure<ClientOptions>(opts =>
        {
            opts.BaseUrl = baseUrl;
            opts.ApiKey = apiKey ?? "";
        });

        services.AddHttpClient<GPClient>();

        return services;
    }
}
