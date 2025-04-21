// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using CommonDotNet.Models;

namespace EmbeddingGenerator.Models;
#pragma warning disable CA2201

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
                MaxDimensions = model.Value.MaxDimensions,
                SupportsCustomDimensions = model.Value.SupportsCustomDimensions
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
                Model = model.Value.Deployment,
                MaxDimensions = model.Value.MaxDimensions,
                SupportsCustomDimensions = model.Value.SupportsCustomDimensions
            };

            modelInfo.EnsureValid();

            list[model.Key] = modelInfo;
        }

        return list;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return this.OpenAI.Validate(null!).Concat(this.AzureAI.Validate(null!));
    }
}
