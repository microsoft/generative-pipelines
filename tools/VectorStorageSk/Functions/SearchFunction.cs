// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using VectorStorageSk.Models;
using VectorStorageSk.SemanticKernel;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Functions;

/*
 * !!!! IMPORTANT !!!!
 *
 * Due to the design of VectorStoreGenericDataModel and the available query translators,
 * filter Expressions defined on "VectorStoreGenericDataModel" are not supported (exceptions are thrown).
 *
 * For now, for every schema required, define a POCO class under DataTypes folder.
 * See the existing MemoryRecord class for reference.
 * Depending on storage, the Key might be Guid or string, so MemoryRecord is a generic class.
 *
 * When invoking /search you must pass these parameters:
 *
 * - [MANDATORY] dataType: the class name of the POCO class, e.g. "MemoryRecord"
 * - [OPTIONAL] primaryKeyType: the type of the primary key, e.g. "string", "Guid", "number"
 *
 * Example:
 *
 *      POST /search
 *      {
 *          storageType:    "qdrant",
 *          collection:     "memories",
 *          dataType:       "MemoryRecord",
 *          primaryKeyType: "default"
 *      }
 *
 * Once Expressions on VectorStoreGenericDataModel are supported, custom POCO classes
 * and `dataType` won't be required anymore.
 */

internal sealed class SearchFunction<TKey, TRecord> where TKey : notnull
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SearchFunction<object, object>> _log;

    public SearchFunction(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._sp = sp;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<SearchFunction<object, object>>();
    }

    public async Task<IResult> InvokeAsync(SearchRequest req, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Starting {FunctionName}", this.GetType().Name);
        this._log.LogDebug("Search request on collection: {Collection}; storage type {StorageType}; DataType: {DataType}; PK: {PKType}; filter: {Filter}",
            req.CollectionName, req.StorageType, req.DataType, req.PrimaryKeyType, req.Filter);

        if (req.Validate() is { } error) { return Results.BadRequest(error); }

        (IVectorStore? vectorStore, IResult? err) = StorageLib.GetVectorStore(req.StorageType, this._sp, this._log);
        if (err != null) { return err; }

        if (vectorStore == null)
        {
            this._log.LogError("Unable to instantiate client for storage type {StorageType}", req.StorageType.ToString("G"));
            return Results.InternalServerError("Unable to instantiate vector store");
        }

        // Vector required by SK for vector search
        FieldDefinition vectorField = req.Fields.First(f => f.Type == FieldTypes.Embedding);
        var vector = (req.Vector == null) ? new Embedding(new float[vectorField.VectorSize ?? 1]) : new Embedding(req.Vector);

        // VectorStoreRecordDefinition recordDefinition = StorageLib.PrepareRecordDefinition(req.Fields, vectorStore);

        try
        {
            VectorSearchResults<TRecord> results = await this.SearchAsync(vectorStore, req, vector, null!, cancellationToken).ConfigureAwait(false);
            if (results == null) { return Results.InternalServerError("Storage type not supported"); }

            var response = new SearchResponse();
            await foreach (VectorSearchResult<TRecord> x in results.Results.WithCancellation(cancellationToken))
            {
                if (x.Record == null) { continue; }

                response.Results.Add(new SearchResponse.SearchResult
                {
                    Record = x.Record,
                    Score = x.Score is > -100000 ? x.Score.Value : 0 // workaround for JSON serialization issue
                });
            }

            return Results.Ok(response);
        }
        catch (VectorStoreOperationException e) when (Regex.IsMatch(e.Message, @"collection '.*' does not exist", RegexOptions.IgnoreCase))
        {
            return Results.BadRequest($"Collection {req.CollectionName} not found");
        }
    }

    private async Task<VectorSearchResults<TRecord>> SearchAsync(
        IVectorStore store, SearchRequest req, Embedding vector, VectorStoreRecordDefinition def, CancellationToken ct)
    {
        // var collection = store.GetCollection<TKey, TRecord>(req.CollectionName, def);
        var collection = store.GetCollection<TKey, TRecord>(req.CollectionName);

        // Decide if we need to use the hybrid search or the vector search
        if (req.Keywords is { Length: > 0 })
        {
            if (collection is not IKeywordHybridSearch<TRecord> hybrid)
            {
                throw new InvalidOperationException($"Hybrid search not supported for {req.StorageType}");
            }

            this._log.LogDebug("Choosing hybrid search, Top: {Top}, Skip: {Skip}, Keywords: {Keywords}, Filter: {Filter}", req.Top, req.Skip, req.Keywords.Length, req.Filter);
            var options = this.GetHybridSearchOptions(req);
            return await hybrid.HybridSearchAsync<Embedding>(vector, req.Keywords, options, ct).ConfigureAwait(false);
        }
        else
        {
            this._log.LogDebug("Choosing vector search, Top: {Top}, Skip: {Skip}, Filter: {Filter}", req.Top, req.Skip, req.Filter);
            var options = this.GetVectorSearchOptions(req);
            return await collection.VectorizedSearchAsync(vector, options, ct).ConfigureAwait(false);
        }
    }

    private HybridSearchOptions<TRecord> GetHybridSearchOptions(SearchRequest req)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(req.Filter);

        return new HybridSearchOptions<TRecord>
        {
            Top = req.Top ?? 10,
            Skip = req.Skip ?? 0,
            IncludeVectors = true,
            Filter = ODataFilterTranslator.BuildFilterExpression<TRecord>(req.Filter, this._log),
        };
    }

    private VectorSearchOptions<TRecord> GetVectorSearchOptions(SearchRequest req)
    {
        return new VectorSearchOptions<TRecord>
        {
            Top = req.Top ?? 10,
            Skip = req.Skip ?? 0,
            IncludeVectors = true,
            Filter = ODataFilterTranslator.BuildFilterExpression<TRecord>(req.Filter, this._log),
        };
    }
}
