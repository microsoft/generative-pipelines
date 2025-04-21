// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace EmbeddingGenerator.Models;

internal sealed class OpenAIModelProviderConfig : IValidatableObject
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, OpenAIModelConfig> Models { get; set; } = new();

    public OpenAIModelConfig GetModelById(string modelId)
    {
        if (this.Models.TryGetValue(modelId, out OpenAIModelConfig? model))
        {
            if (string.IsNullOrWhiteSpace(model.ApiKey))
            {
                model.ApiKey = this.ApiKey;
            }

            return model;
        }

        throw new ApplicationException($"OpenAI model {modelId} not found");
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return this.Models.SelectMany(x => x.Value.Validate(null!));
    }
}
