// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using CommonDotNet.Diagnostics;
using EmbeddingGenerator.Config;
using EmbeddingGenerator.Functions;
using EmbeddingGenerator.Models;
using OpenAI;
using OpenAI.Embeddings;

namespace EmbeddingGenerator.Client;

internal static class ClientFactory
{
    /// <summary>
    /// Get custom embedding client using credentials inlined in the HTTP request
    /// </summary>
    public static EmbeddingClient GetEmbeddingClient(
        CustomEmbeddingRequest req,
        ILoggerFactory loggerFactory)
    {
        switch (req.Provider)
        {
            case CustomEmbeddingRequest.ModelProviders.AzureAI:
                return GetEmbeddingClient(new AzureAIDeploymentConfig
                {
                    Endpoint = req.Endpoint,
                    Auth = req.Auth,
                    ApiKey = req.ApiKey,
                    Deployment = req.Deployment,
                    MaxDimensions = req.MaxDimensions,
                    SupportsCustomDimensions = req.SupportsCustomDimensions
                }, loggerFactory);

            case CustomEmbeddingRequest.ModelProviders.OpenAI:
                return GetEmbeddingClient(new OpenAIModelConfig
                {
                    Endpoint = req.Endpoint,
                    ApiKey = req.ApiKey,
                    Model = req.Model,
                    MaxDimensions = req.MaxDimensions,
                    SupportsCustomDimensions = req.SupportsCustomDimensions
                }, loggerFactory);

            default:
                throw new ArgumentOutOfRangeException(nameof(req.Provider), $"Model provider '{req.Provider}' is not supported");
        }
    }

    /// <summary>
    /// Get OpenAI embedding client
    /// </summary>
    public static EmbeddingClient GetEmbeddingClient(OpenAIModelConfig cfg, ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ClientFactory");
        log.LogInformation("Preparing embedding client for OpenAI model {Model}", cfg.Model);

        var options = new OpenAIClientOptions
        {
            UserAgentApplicationId = Telemetry.HttpUserAgent
        };

        if (!string.IsNullOrWhiteSpace(cfg.Endpoint))
        {
            options.Endpoint = new Uri(cfg.Endpoint);
        }

        try
        {
            EmbeddingClient client = new(cfg.Model, new ApiKeyCredential(cfg.ApiKey!), options);
            return client;
        }
        catch (ArgumentException e) when (e.Message.Contains("empty string", StringComparison.OrdinalIgnoreCase)
                                          && e.Message.Contains("Parameter 'key'", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException("The embedding model API Key is missing", e);
        }
    }

    /// <summary>
    /// Get Azure AI embedding client
    /// </summary>
    public static EmbeddingClient GetEmbeddingClient(AzureAIDeploymentConfig cfg, ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ClientFactory");
        log.LogInformation("Preparing embedding client for Azure AI deployment {Deployment}", cfg.Deployment);

        var endpoint = new Uri(cfg.Endpoint ?? throw new BadRequestException("Azure AI endpoint value is empty"));

        var options = new AzureOpenAIClientOptions
        {
            UserAgentApplicationId = Telemetry.HttpUserAgent
        };

        AzureOpenAIClient azureClient;
        switch (cfg.Auth)
        {
            case AuthTypes.DefaultAzureCredential:
            default:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new DefaultAzureCredential(),
                    options);
                break;

            case AuthTypes.ApiKey:
                var apiKey = cfg.ApiKey ?? throw new BadRequestException("Azure AI API Key value is empty");
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new ApiKeyCredential(apiKey),
                    options);
                break;

            case AuthTypes.AzureCliCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new AzureCliCredential(),
                    options);
                break;

            case AuthTypes.AzureDeveloperCliCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new AzureDeveloperCliCredential(),
                    options);
                break;

            case AuthTypes.AzurePowerShellCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new AzurePowerShellCredential(),
                    options);
                break;

            case AuthTypes.EnvironmentCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new EnvironmentCredential(),
                    options);
                break;

            case AuthTypes.InteractiveBrowserCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new InteractiveBrowserCredential(),
                    options);
                break;

            case AuthTypes.ManagedIdentityCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new ManagedIdentityCredential(),
                    options);
                break;

            case AuthTypes.VisualStudioCodeCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new VisualStudioCodeCredential(),
                    options);
                break;

            case AuthTypes.VisualStudioCredential:
                azureClient = new AzureOpenAIClient(
                    endpoint: endpoint,
                    credential: new VisualStudioCredential(),
                    options);
                break;
        }

        EmbeddingClient client = azureClient.GetEmbeddingClient(cfg.Deployment);
        return client;
    }
}
