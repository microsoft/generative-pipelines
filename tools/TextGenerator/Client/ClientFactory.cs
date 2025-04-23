// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using CommonDotNet.Diagnostics;
using CommonDotNet.Http;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using OpenAI;
using TextGenerator.Config;
using TextGenerator.Models;

namespace TextGenerator.Client;

internal static class ClientFactory
{
    // TODO remove hard coded value
    private const int MaxRetries = 3;

    /// <summary>
    /// Instantiate Azure OpenAI client
    /// </summary>
    public static IChatClient GetChatClient(
        AzureAIDeploymentConfig cfg,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ClientFactory");
        log.LogInformation("Preparing chat client for Azure OpenAI deployment {Deployment}", cfg.Deployment);

        AzureOpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
        };

        var endpoint = new Uri(cfg.Endpoint ?? throw new BadRequestException("Azure AI endpoint value is empty"));
        AzureOpenAIClient azureClient;

        switch (cfg.Auth)
        {
            case AuthTypes.Unknown:
                throw new ConfigurationException($"Unknown auth '{cfg.Auth}' mechanism for Azure OpenAI");

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

        return new AzureOpenAIChatCompletionService(cfg.Deployment, azureClient, loggerFactory: loggerFactory).AsChatClient();
    }

    /// <summary>
    /// Instantiate OpenAI client
    /// </summary>
    public static IChatClient GetChatClient(OpenAIModelConfig cfg, ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ClientFactory");
        log.LogInformation("Preparing chat client for OpenAI model {Deployment}", cfg.Model);

        OpenAIClientOptions options = new()
        {
            RetryPolicy = new ClientSequentialRetryPolicy(maxRetries: Math.Max(0, MaxRetries), loggerFactory),
            UserAgentApplicationId = Telemetry.HttpUserAgent,
        };

        var apiKey = cfg.ApiKey ?? throw new BadRequestException("OpenAI API Key value is empty");
        var openAiclient = new OpenAIClient(new ApiKeyCredential(apiKey), options);

        return new OpenAIChatCompletionService(cfg.Model, openAiclient, loggerFactory: loggerFactory).AsChatClient();
    }

    /// <summary>
    /// Instantiate Ollama client
    /// </summary>
    public static IChatClient GetOllamaChatService(string? endpoint, string modelId, ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ClientFactory");
        log.LogInformation("Preparing chat client for Ollama model {Model}", modelId);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new BadRequestException("Unable to connect to Ollama, the 'HTTP endpoint' information is missing");
        }

        return new OllamaApiClient(endpoint, modelId);
    }
}
