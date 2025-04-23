// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace EmbeddingGenerator.Config;

internal sealed class OpenAIModelConfig : AIModelConfig, IValidatableObject
{
    public string? Endpoint { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Model { get; set; } = string.Empty;

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

        if (string.IsNullOrWhiteSpace(this.Model))
        {
            yield return new ValidationResult("The model name is required, the value is empty", [nameof(this.Model)]);
        }

        if (this.MaxDimensions < 1)
        {
            yield return new ValidationResult("The embedding max dimensions cannot be less than 1", [nameof(this.MaxDimensions)]);
        }
    }
}
