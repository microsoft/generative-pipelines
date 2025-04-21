// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Models;
using EmbeddingGenerator.Embeddings;
using EmbeddingGenerator.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Embeddings;

namespace EmbeddingGenerator.Functions;

internal sealed class EmbeddingFunction
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<EmbeddingFunction> _log;

    public EmbeddingFunction(AppConfig appConfig, ILoggerFactory? lf = null)
    {
        this._appConfig = appConfig;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<EmbeddingFunction>();
    }

    public async Task<IResult> InvokeAsync(EmbeddingRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null) { return Results.BadRequest("The request is empty"); }

        if (!req.FixState().IsValid(out var errMsg)) { return Results.BadRequest(errMsg); }

        var models = this._appConfig.GetModelsInfo();
        if (!models.TryGetValue(req.ModelId, out ModelInfo? model))
        {
            return Results.BadRequest($"Embedding model {req.ModelId} is not available. Check /models for the list of available models.");
        }

        model.EnsureValid();

        EmbeddingClient client;
        AIModelConfig modelSettings;
        switch (model.Provider)
        {
            case ModelInfo.ModelProviders.OpenAI:
                modelSettings = this._appConfig.OpenAI.GetModelById(req.ModelId);
                client = ClientFactory.GetClient((OpenAIModelConfig)modelSettings, this._log);
                break;

            case ModelInfo.ModelProviders.AzureAI:
                modelSettings = this._appConfig.AzureAI.GetModelById(req.ModelId);
                client = ClientFactory.GetClient((AzureAIDeploymentConfig)modelSettings, this._log);
                break;

            default:
                return Results.InternalServerError($"Provider value {model.Provider:G} is not supported.");
        }

        return await EmbeddingFunctionBase.InvokeAsync(
            client,
            req.Input,
            req.Inputs,
            modelSettings.SupportsCustomDimensions,
            req.Dimensions,
            cancellationToken).ConfigureAwait(false);
    }
}
