// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Models;
using EmbeddingGenerator.Client;
using EmbeddingGenerator.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmbeddingGenerator.Functions;

internal sealed class CustomEmbeddingFunction
{
    private readonly ILogger<CustomEmbeddingFunction> _log;
    private readonly ILoggerFactory _loggerFactory;

    public CustomEmbeddingFunction(AppConfig appConfig, ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this._log = this._loggerFactory.CreateLogger<CustomEmbeddingFunction>();
    }

    public async Task<IResult> InvokeAsync(CustomEmbeddingRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null) { return Results.BadRequest("The request is empty"); }

        if (!req.FixState().IsValid(out var errMsg)) { return Results.BadRequest(errMsg); }

        var client = ClientFactory.GetEmbeddingClient(req, this._loggerFactory);

        return await EmbeddingFunctionBase.InvokeAsync(
            client,
            req.Input,
            req.Inputs,
            req.SupportsCustomDimensions,
            req.Dimensions,
            cancellationToken).ConfigureAwait(false);
    }
}
