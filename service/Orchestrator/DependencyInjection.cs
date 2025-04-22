// Copyright (c) Microsoft. All rights reserved.

using System.Net.Mime;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Net.Http.Headers;
using Orchestrator.Config;
using Orchestrator.Diagnostics;
using Orchestrator.Http;
using Orchestrator.Orchestration;
using Orchestrator.ServiceDiscovery;
using Orchestrator.Storage;

namespace Orchestrator;

#pragma warning disable CA1724
public static partial class DependencyInjection
{
    public static IHostApplicationBuilder AddOrchestrationWorkspace(
        this IHostApplicationBuilder builder,
        string blobStorageName = "blobstorage")
    {
        var workspaceConfig = builder.Configuration.GetSection("App:Workspace").Get<WorkspaceConfig>()?.Validate()
                              ?? throw new ApplicationException(nameof(WorkspaceConfig) + " not available");

        builder.Services.AddSingleton(workspaceConfig);
        builder.Services.AddSingleton<SimpleWorkspace>();
        if (workspaceConfig.UseFileSystem)
        {
            builder.Services.AddSingleton<IFileSystem, FileSystem>();
            return builder;
        }

        AzureBlobFileSystemConfig azureBlobFileSystemConfig;
        try
        {
            azureBlobFileSystemConfig = builder.Configuration.GetSection("App:Workspace").Get<AzureBlobFileSystemConfig>()?.Validate()
                                        ?? throw new ApplicationException(nameof(AzureBlobFileSystemConfig) + " not available");
        }
        catch (Exception e)
        {
            throw new ConfigurationException("Unable to load App:Workspace configuration", e);
        }

        builder.Services.AddSingleton(azureBlobFileSystemConfig);
        builder.Services.AddSingleton<IFileSystem, AzureBlobFileSystem>();

        // Using Aspire client
        builder.AddAzureBlobClient(blobStorageName,
            configureSettings: settings =>
            {
                switch (azureBlobFileSystemConfig.Auth)
                {
                    default:
                    case AzureBlobFileSystemConfig.AzureAuthTypes.Unknown:
                    case AzureBlobFileSystemConfig.AzureAuthTypes.ConnectionString:
                        // noop, use defaults
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.DefaultAzureCredential:
                        settings.Credential = new DefaultAzureCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.AzureCliCredential:
                        settings.Credential = new AzureCliCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.AzureDeveloperCliCredential:
                        settings.Credential = new AzureDeveloperCliCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.AzurePowerShellCredential:
                        settings.Credential = new AzurePowerShellCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.EnvironmentCredential:
                        settings.Credential = new EnvironmentCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.InteractiveBrowserCredential:
                        settings.Credential = new InteractiveBrowserCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.ManagedIdentityCredential:
                        settings.Credential = new ManagedIdentityCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.VisualStudioCodeCredential:
                        settings.Credential = new VisualStudioCodeCredential();
                        break;

                    case AzureBlobFileSystemConfig.AzureAuthTypes.VisualStudioCredential:
                        settings.Credential = new VisualStudioCredential();
                        break;
                }
            },
            configureClientBuilder: b => b.ConfigureOptions(o => o.Diagnostics.ApplicationId = Telemetry.HttpUserAgent)
        );

        return builder;
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
