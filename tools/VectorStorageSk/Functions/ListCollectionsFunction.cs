// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using VectorStorageSk.Models;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Functions;

internal sealed class ListCollectionsFunction
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ListCollectionsFunction> _log;

    public ListCollectionsFunction(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._sp = sp;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<ListCollectionsFunction>();
    }

    public async Task<IResult> InvokeAsync(ListCollectionsRequest req, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Starting {FunctionName}", this.GetType().Name);

        if (req.Validate() is { } error) { return Results.BadRequest(error); }

        (IVectorStore? vectorStore, IResult? err) = StorageLib.GetVectorStore(req.StorageType, this._sp, this._log);
        if (err != null) { return err; }

        if (vectorStore == null)
        {
            this._log.LogError("Unable to instantiate client for storage type {StorageType}", req.StorageType.ToString("G"));
            return Results.InternalServerError("Unable to instantiate vector store");
        }

        var result = new List<string>();
        var list = vectorStore.ListCollectionNamesAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var collection in list.ConfigureAwait(false))
        {
            result.Add(collection);
        }

        return Results.Ok(result);
    }
}
