// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using CommonDotNet.Models;

namespace TextGenerator.Config;

internal sealed class AppConfig : IValidatableObject
{
    public OpenAIModelProviderConfig OpenAI { get; set; } = new();
    public AzureAIModelProviderConfig AzureAI { get; set; } = new();

    public Dictionary<string, ModelInfo> GetModelsInfo()
    {
        // Note: allow case-insensitive lookups
        var list = new Dictionary<string, ModelInfo>(StringComparer.OrdinalIgnoreCase);

        // OpenAI models
        foreach (KeyValuePair<string, OpenAIModelConfig> model in this.OpenAI.Models)
        {
            var modelInfo = new ModelInfo
            {
                ModelId = model.Key,
                Provider = ModelInfo.ModelProviders.OpenAI,
                Model = model.Value.Model,
                ContextWindow = model.Value.ContextWindow,
                MaxOutputTokens = model.Value.MaxOutputTokens,
            };

            if (!string.IsNullOrWhiteSpace(model.Value.Endpoint))
            {
                modelInfo.Endpoint = model.Value.Endpoint;
            }

            modelInfo.EnsureValid();

            list[model.Key] = modelInfo;
        }

        // Azure AI models
        foreach (KeyValuePair<string, AzureAIDeploymentConfig> model in this.AzureAI.Deployments)
        {
            // Ignore examples from appsettings.json
            if (model.Key == "_example1" || model.Key == "_example2") { continue; }

            var modelInfo = new ModelInfo
            {
                ModelId = model.Key,
                Provider = ModelInfo.ModelProviders.AzureAI,
                Deployment = model.Value.Deployment,
                Endpoint = model.Value.Endpoint,
                ContextWindow = model.Value.ContextWindow,
                MaxOutputTokens = model.Value.MaxOutputTokens,
            };

            modelInfo.EnsureValid();

            list[model.Key] = modelInfo;
        }

        return list;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var x in this.OpenAI.Validate(null!).Concat(this.AzureAI.Validate(null!)))
        {
            yield return x;
        }

        var modelIDs = new HashSet<string>();
        foreach (var key in this.OpenAI.Models.Keys.Concat(this.AzureAI.Deployments.Keys))
        {
            if (!modelIDs.Add(key))
            {
                yield return new ValidationResult($"Duplicate Model ID found: {key}", [nameof(this.OpenAI.Models)]);
            }
        }
    }
}
