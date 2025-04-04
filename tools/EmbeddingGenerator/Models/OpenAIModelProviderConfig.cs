// Copyright (c) Microsoft. All rights reserved.

namespace EmbeddingGenerator.Models;

internal sealed class OpenAIModelProviderConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, OpenAIModelConfig> Models { get; set; } = new();

    public OpenAIModelProviderConfig Validate()
    {
        foreach (var model in this.Models) { model.Value.Validate(); }

        return this;
    }

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
}
