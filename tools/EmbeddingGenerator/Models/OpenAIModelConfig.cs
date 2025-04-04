// Copyright (c) Microsoft. All rights reserved.

namespace EmbeddingGenerator.Models;

internal sealed class OpenAIModelConfig : AIModelConfig
{
    public string? Endpoint { get; set; } // Optional override
    public string? ApiKey { get; set; } // Optional override
    public string Model { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.Model))
        {
            throw new ApplicationException("OpenAI model name is required, the value is empty");
        }

        if (this.MaxDimensions < 1)
        {
            throw new ApplicationException("OpenAI model max dimensions cannot be less than 1");
        }

        if (this.MaxBatchSize < 1)
        {
            this.MaxBatchSize = 1;
        }
    }
}
