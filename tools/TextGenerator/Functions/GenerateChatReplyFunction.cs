// Copyright (c) Microsoft. All rights reserved.

using CommonDotNet.Models;
using Microsoft.Extensions.Logging.Abstractions;
using TextGenerator.Config;

namespace TextGenerator.Functions;

internal sealed class GenerateChatReplyFunction
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<GenerateChatReplyFunction> _log;
    private readonly ILoggerFactory _loggerFactory;

    public GenerateChatReplyFunction(AppConfig appConfig, ILoggerFactory? loggerFactory = null)
    {
        this._appConfig = appConfig;
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this._log = this._loggerFactory.CreateLogger<GenerateChatReplyFunction>();
    }

    public async Task<IResult> InvokeAsync(GenerateChatReplyRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null) { return Results.BadRequest("The request is empty"); }

        if (!req.FixState().IsValid(out var errMsg)) { return Results.BadRequest(errMsg); }

        // var client = ClientFactory.GetClient(req, this._log);

        // return await EmbeddingFunctionBase.InvokeAsync(
        //     client,
        //     req.Input,
        //     req.Inputs,
        //     req.SupportsCustomDimensions,
        //     req.Dimensions,
        //     cancellationToken).ConfigureAwait(false);

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

        return Results.Ok();
    }
}
