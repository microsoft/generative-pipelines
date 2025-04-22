// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using EmbeddingGenerator.Models;

namespace EmbeddingGenerator.Config;

internal sealed class AzureAIModelProviderConfig : IValidatableObject
{
    public string Endpoint { get; set; } = string.Empty; // Optional override
    public AuthTypes Auth { get; set; } = AuthTypes.DefaultAzureCredential; // Optional override
    public string ApiKey { get; set; } = string.Empty; // Optional override
    public Dictionary<string, AzureAIDeploymentConfig> Deployments { get; set; } = new();

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

            if (deployment.Auth.Value == AuthTypes.ApiKey && string.IsNullOrWhiteSpace(deployment.ApiKey))
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return this.Deployments.SelectMany(x => x.Value.Validate(null!));
    }
}
