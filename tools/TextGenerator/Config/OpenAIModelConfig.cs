// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace TextGenerator.Config;

internal sealed class OpenAIModelConfig : AIModelConfig, IValidatableObject
{
    public string? Endpoint { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Model { get; set; } = string.Empty;

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

        if (string.IsNullOrWhiteSpace(this.Model))
        {
            yield return new ValidationResult("The OpenAI model name is required, the value is empty", [nameof(this.Model)]);
        }

        if (this.ContextWindow < 1)
        {
            yield return new ValidationResult("OpenAI model context window cannot be less than 1", [nameof(this.ContextWindow)]);
        }
    }
}
