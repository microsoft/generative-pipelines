// Copyright (c) Microsoft. All rights reserved.

namespace EmbeddingGenerator.Models;

internal sealed class AzureAIDeploymentConfig : AIModelConfig
{
    public string? Endpoint { get; set; } // Optional override
    public AzureAIModelProviderConfig.AzureAuthTypes? Auth { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Deployment { get; set; } = string.Empty;

    public AzureAIDeploymentConfig Validate()
    {
        if (this.MaxDimensions < 1)
        {
            throw new ApplicationException("Azure AI model max dimensions cannot be less than 1");
        }

        if (this.MaxBatchSize < 1)
        {
            this.MaxBatchSize = 1;
        }

        if (string.IsNullOrWhiteSpace(this.Deployment))
        {
            throw new ApplicationException("Azure AI model deployment is required, the value is empty");
        }

        return this;
    }
}
