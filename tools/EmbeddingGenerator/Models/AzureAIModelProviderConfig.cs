﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Models;

internal sealed class AzureAIModelProviderConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AzureAuthTypes
    {
        AzureIdentity,
        APIKey
    }

    public string Endpoint { get; set; } = string.Empty; // Optional override
    public AzureAuthTypes Auth { get; set; } = AzureAuthTypes.AzureIdentity; // Optional override
    public string ApiKey { get; set; } = string.Empty; // Optional override
    public Dictionary<string, AzureAIDeploymentConfig> Deployments { get; set; } = new();

    public AzureAIModelProviderConfig Validate()
    {
        foreach (var model in this.Deployments) { model.Value.Validate(); }

        return this;
    }

    public AzureAIDeploymentConfig GetModelById(string modelId)
    {
        if (this.Deployments.TryGetValue(modelId, out AzureAIDeploymentConfig? deployment))
        {
            if (string.IsNullOrWhiteSpace(deployment.Endpoint))
            {
                deployment.Endpoint = this.Endpoint;
                if (string.IsNullOrWhiteSpace(deployment.Endpoint))
                {
                    throw new ApplicationException("Azure AI model endpoint is required, the value is empty");
                }
            }

            if (!deployment.Auth.HasValue)
            {
                deployment.Auth = this.Auth;
                if (!deployment.Auth.HasValue)
                {
                    throw new ApplicationException("Azure AI model auth type is required, the value is empty");
                }
            }

            if (deployment.Auth.Value == AzureAuthTypes.APIKey && string.IsNullOrWhiteSpace(deployment.ApiKey))
            {
                deployment.ApiKey = this.ApiKey;
                if (string.IsNullOrWhiteSpace(deployment.ApiKey))
                {
                    throw new ApplicationException("Azure AI model API key is required, the value is empty");
                }
            }

            return deployment;
        }

        throw new ApplicationException($"Azure deployment {modelId} not found");
    }
}
