// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace EmbeddingGenerator.Models;

internal sealed class AzureAIDeploymentConfig : AIModelConfig, IValidatableObject
{
    public string? Endpoint { get; set; } // Optional override
    public AzureAIModelProviderConfig.AzureAuthTypes? Auth { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Deployment { get; set; } = string.Empty;

    public void FixState()
    {
        if (this.MaxBatchSize < 1)
        {
            this.MaxBatchSize = 1;
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        this.FixState();

        if (string.IsNullOrWhiteSpace(this.Deployment))
        {
            yield return new ValidationResult("The Azure deployment name is required, the value is empty", [nameof(this.Deployment)]);
        }

        if (this.MaxDimensions < 1)
        {
            yield return new ValidationResult("The embedding max dimensions cannot be less than 1", [nameof(this.MaxDimensions)]);
        }
    }
}
