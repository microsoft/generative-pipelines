// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using CommonDotNet.Diagnostics;
using EmbeddingGenerator.Functions;
using EmbeddingGenerator.Models;
using OpenAI;
using OpenAI.Embeddings;

namespace EmbeddingGenerator.Embeddings;

internal static class ClientFactory
{
    /// <summary>
    /// Get OpenAI embedding client
    /// </summary>
    public static EmbeddingClient GetClient(OpenAIModelConfig cfg, ILogger log)
    {
        log.LogInformation("Preparing client for OpenAI model {Model}", cfg.Model);

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
    public static EmbeddingClient GetClient(AzureAIDeploymentConfig cfg, ILogger log)
    {
        log.LogInformation("Preparing client for Azure AI deployment {Deployment}", cfg.Deployment);
        var endpoint = new Uri(cfg.Endpoint ?? throw new ArgumentNullException(nameof(cfg.Endpoint), "Azure AI endpoint value is empty"));

        var options = new AzureOpenAIClientOptions
        {
            UserAgentApplicationId = Telemetry.HttpUserAgent
        };

        AzureOpenAIClient azureClient = cfg.Auth switch
        {
            AzureAIModelProviderConfig.AzureAuthTypes.AzureIdentity => new(endpoint, new DefaultAzureCredential(), options),
            AzureAIModelProviderConfig.AzureAuthTypes.APIKey => new(endpoint, new ApiKeyCredential(cfg.ApiKey!), options),
            _ => throw new ArgumentOutOfRangeException(nameof(cfg.Auth), "Auth type value is not supported")
        };

        EmbeddingClient client = azureClient.GetEmbeddingClient(cfg.Deployment);
        return client;
    }

    /// <summary>
    /// Get custom embedding client using inline credentials
    /// </summary>
    public static EmbeddingClient GetClient(CustomEmbeddingRequest req, ILogger log)
    {
        switch (req.Provider)
        {
            case CustomEmbeddingRequest.ModelProviders.AzureAI:
                return GetClient(new AzureAIDeploymentConfig
                {
                    Endpoint = req.Endpoint,
                    Auth = req.Auth == CustomEmbeddingRequest.AuthTypes.AzureIdentity ? AzureAIModelProviderConfig.AzureAuthTypes.AzureIdentity : AzureAIModelProviderConfig.AzureAuthTypes.APIKey,
                    ApiKey = req.ApiKey,
                    Deployment = req.ModelId,
                    MaxDimensions = req.MaxDimensions,
                    SupportsCustomDimensions = req.SupportsCustomDimensions
                }, log);

            case CustomEmbeddingRequest.ModelProviders.OpenAI:
                return GetClient(new OpenAIModelConfig
                {
                    Model = req.ModelId,
                    Endpoint = req.Endpoint,
                    ApiKey = req.ApiKey,
                    MaxDimensions = req.MaxDimensions,
                    SupportsCustomDimensions = req.SupportsCustomDimensions
                }, log);

            default:
                throw new ArgumentOutOfRangeException(nameof(req.Provider), $"Model provider '{req.Provider}' is not supported");
        }
    }
}
