// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using CommonDotNet.Diagnostics;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Npgsql;

namespace VectorStorageSk.Storage;

internal static class DependencyInjection
{
    private const string PostgresInitSQL = "CREATE EXTENSION vector;";

    public static void AddAzureAiSearchVectorStore(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAzureSearchClient(connectionName: connectionName,
            configureClientBuilder: x =>
            {
                x.ConfigureOptions(o =>
                {
                    o.Diagnostics.ApplicationId = Telemetry.HttpUserAgent;
                    o.Diagnostics.IsTelemetryEnabled = Telemetry.IsTelemetryEnabled;
                });
            });

        builder.Services.AddSingleton<AzureAISearchVectorStore>(sp =>
        {
            // TODO: revisit to use conn strings and keyed services
            const string Section = "App:VectorStores:AzureAISearch";

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var log = loggerFactory.CreateLogger(nameof(AddAzureAiSearchVectorStore));

            var config = sp.GetService<IConfiguration>()?.GetSection(Section).Get<AzureAISearchConfig>() ?? new AzureAISearchConfig();
            config.Deployments ??= new Dictionary<string, AzureAISearchConfig.AzureAISearchDeploymentConfig>();

            // Note: appsettings take precedence over the client injected via service provider
            if (config.Deployments.TryGetValue("default", out var deploymentConfig))
            {
                return BuildAzureAISearchVectorStore("default", deploymentConfig, log)
                       ?? throw new InvalidOperationException($"Unable to load Azure AI Search 'default' vector store from {Section}");
            }

            var searchClient = sp.GetService<SearchIndexClient>();
            if (searchClient != null) { return new AzureAISearchVectorStore(searchClient); }

            log.LogError("Default Azure AI Search client or settings not found");
            throw new InvalidOperationException($"Unable to load Azure AI Search");
        });
    }

    public static void AddInMemoryVectorStore(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<InMemoryVectorStore>();
    }

    public static void AddPostgresVectorStore(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDataSource(connectionName, configureDataSourceBuilder: dsBuilder => dsBuilder.UseVector());
        builder.Services.AddSingleton<PostgresVectorStore>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(AddPostgresVectorStore));

            try
            {
                using var cmd = dataSource.CreateCommand(PostgresInitSQL);
                cmd.ExecuteNonQuery();
                logger.LogInformation("Postgres pgvector extension enabled");
            }
            catch (Exception e) when (e.Message.Contains("extension \"vector\" already exists"))
            {
                logger.LogWarning("Postgres pgvector extension already enabled, ignoring SQL error");
            }
            catch (Exception e) when (e.Message.Contains("extension \"vector\" is not available"))
            {
                logger.LogCritical(e, "Postgres pgvector extension is not available on this system");
            }
#pragma warning disable CA1031

            catch (Exception e)
            {
                logger.LogCritical(e, "Error initializing Postgres pgvector");
            }
#pragma warning restore CA1031

            return new PostgresVectorStore(dataSource);
        });
    }

    public static void AddQdrantVectorStore(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddQdrantClient(connectionName: connectionName, configureSettings: x => { });
        // builder.Services.AddSingleton<QdrantVectorStoreOptions>(new QdrantVectorStoreOptions
        // {
        //     HasNamedVectors = false,
        //     FilterTranslator = new MyQdrantFilterTranslator()
        // });
        builder.Services.AddSingleton<QdrantVectorStore>();
    }

    // TODO: revisit to use conn strings and keyed services
    private static AzureAISearchVectorStore? BuildAzureAISearchVectorStore(
        string deploymentName,
        AzureAISearchConfig.AzureAISearchDeploymentConfig deploymentConfig,
        ILogger log)
    {
        if (!deploymentConfig.IsValid(log))
        {
            log.LogError("Deployment '{Deployment}' configuration is invalid", deploymentName);
            return null;
        }

        var options = new SearchClientOptions
        {
            Diagnostics =
            {
                ApplicationId = Telemetry.HttpUserAgent,
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
            }
        };

        SearchIndexClient searchClient;
        switch (deploymentConfig.Auth)
        {
            case AzureAuthTypes.AzureIdentity:
            {
                if (!string.IsNullOrWhiteSpace(deploymentConfig.AzureIdentityAudience))
                {
                    options.Audience = new SearchAudience(deploymentConfig.AzureIdentityAudience);
                }

                log.LogTrace("Creating Azure AI Search client with AzureIdentity authentication");
                searchClient = new SearchIndexClient(new Uri(deploymentConfig.Endpoint), new DefaultAzureCredential(), options);
                break;
            }

            case AzureAuthTypes.ApiKey:
            {
                log.LogTrace("Creating Azure AI Search client with API Key authentication");
                searchClient = new SearchIndexClient(new Uri(deploymentConfig.Endpoint), new AzureKeyCredential(deploymentConfig.ApiKey), options);
                break;
            }

            default:
                log.LogError("Unsupported authentication type '{Auth}' for Azure AI Search", deploymentConfig.Auth);
                return null;
        }

        log.LogTrace("Creating Azure AI Search vector store SK client");
        return new AzureAISearchVectorStore(searchClient);
    }
}
