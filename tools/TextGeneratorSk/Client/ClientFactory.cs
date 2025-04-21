// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using OpenAI;
using TextGeneratorSk.Models;

namespace TextGeneratorSk.Client;

internal static class ClientFactory
{
    // TODO remove hard coded value
    private const int MaxRetries = 3;

    /// <summary>
    /// Instantiate Azure OpenAI client
    /// </summary>
    public static IChatCompletionService GetChatService(
        AzureAIDeploymentConfig cfg,
        ILoggerFactory? loggerFactory = null)
    {
        AzureOpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
        };

        var endpoint = cfg.Endpoint ?? throw new BadRequestException("Azure AI endpoint value is empty");
        AzureOpenAIClient azureClient;

        switch (cfg.Auth)
        {
            case AzureAIModelProviderConfig.AzureAuthTypes.AzureIdentity:
                azureClient = new AzureOpenAIClient(
                    endpoint: new Uri(endpoint),
                    credential: new DefaultAzureCredential(),
                    options);
                break;

            case AzureAIModelProviderConfig.AzureAuthTypes.APIKey:
                var apiKey = cfg.ApiKey ?? throw new BadRequestException("Azure AI API Key value is empty");
                azureClient = new AzureOpenAIClient(
                    endpoint: new Uri(endpoint),
                    credential: new ApiKeyCredential(apiKey),
                    options);
                break;

            default:
                throw new ConfigurationException($"Unknown auth '{cfg.Auth}' mechanism for Azure OpenAI");
        }

        return new AzureOpenAIChatCompletionService(cfg.Deployment, azureClient, loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Instantiate OpenAI client
    /// </summary>
    public static IChatCompletionService GetChatService(OpenAIModelConfig cfg, ILoggerFactory? loggerFactory)
    {
        OpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
        };

        var apiKey = cfg.ApiKey ?? throw new BadRequestException("OpenAI API Key value is empty");
        var openAiclient = new OpenAIClient(new ApiKeyCredential(apiKey), options);

        return new OpenAIChatCompletionService(cfg.Model, openAiclient, loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Instantiate Ollama client
    /// </summary>
    public static IChatCompletionService GetOllamaChatService(string? endpoint, string modelId, out IDisposable disposable)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new BadRequestException("Unable to connect to Ollama, the 'HTTP endpoint' information is missing");
        }

        var client = new OllamaApiClient(endpoint, modelId);
        disposable = client;
        return client.AsChatCompletionService();
    }
}
