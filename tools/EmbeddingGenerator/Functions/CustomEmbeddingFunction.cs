// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Models;
using EmbeddingGenerator.Embeddings;
using EmbeddingGenerator.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmbeddingGenerator.Functions;

internal sealed class CustomEmbeddingFunction
{
    private readonly ILogger<CustomEmbeddingFunction> _log;

    public CustomEmbeddingFunction(AppConfig appConfig, ILoggerFactory? lf = null)
    {
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<CustomEmbeddingFunction>();
    }

    public async Task<IResult> InvokeAsync(CustomEmbeddingRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null) { return Results.BadRequest("The request is empty"); }

        if (!req.FixState().IsValid(out var errMsg)) { return Results.BadRequest(errMsg); }

        var client = ClientFactory.GetClient(req, this._log);

        return await EmbeddingFunctionBase.InvokeAsync(
            client,
            req.Input,
            req.Inputs,
            req.SupportsCustomDimensions,
            req.Dimensions,
            cancellationToken).ConfigureAwait(false);
    }
}
