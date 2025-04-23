// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using TextGenerator.Models;

namespace TextGenerator.Config;

internal sealed class AzureAIDeploymentConfig : AIModelConfig, IValidatableObject
{
    public string? Endpoint { get; set; } // Optional override
    public AuthTypes? Auth { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Deployment { get; set; } = string.Empty;

    public void FixState()
    {
        if (this.MaxOutputTokens < 1)
        {
            this.MaxOutputTokens = 1;
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        this.FixState();

        if (string.IsNullOrWhiteSpace(this.Deployment))
        {
            yield return new ValidationResult("The Azure AI deployment name is required, the value is empty", [nameof(this.Deployment)]);
        }

        if (this.ContextWindow < 1)
        {
            yield return new ValidationResult("The Azure AI model context window cannot be less than 1", [nameof(this.ContextWindow)]);
        }
    }
}
