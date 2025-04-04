// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using VectorStorageSk.Models;
using VectorStorageSk.SemanticKernel;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Functions;

#pragma warning disable CA1031
internal sealed class CreateCollectionFunction
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<CreateCollectionFunction> _log;

    public CreateCollectionFunction(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._sp = sp;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<CreateCollectionFunction>();
    }

    public async Task<IResult> InvokeAsync(CreateCollectionRequest req, bool failOnConflict, CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Starting {FunctionName}, FailOnConflict: {failOnConflict}", this.GetType().Name, failOnConflict);

        if (req.Validate() is { } error) { return Results.BadRequest(error); }

        (IVectorStore? vectorStore, IResult? err) = StorageLib.GetVectorStore(req.StorageType, this._sp, this._log);
        if (err != null) { return err; }

        if (vectorStore == null)
        {
            this._log.LogError("Unable to instantiate client for storage type {StorageType}", req.StorageType.ToString("G"));
            return Results.InternalServerError("Unable to instantiate vector store");
        }

        VectorStoreRecordDefinition recordDefinition;
        try
        {
            this._log.LogDebug("Preparing record definition (for collection deletion)");
            recordDefinition = StorageLib.PrepareRecordDefinition(req.Fields, vectorStore);
        }
        catch (Exception e)
        {
            this._log.LogError("Invalid request: {Message}", e.Message);
            return Results.BadRequest(e.Message);
        }

        try
        {
            this._log.LogDebug("Creating new collection {CollectionName}", req.CollectionName);
            switch (vectorStore)
            {
                case QdrantVectorStore:
                {
                    var collection = vectorStore.GetCollection<Guid, MemoryRecord<Guid>>(req.CollectionName, recordDefinition);
                    if (failOnConflict)
                    {
                        await collection.CreateCollectionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await collection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                    }

                    break;
                }
                case PostgresVectorStore:
                case AzureAISearchVectorStore:
                {
                    var collection = vectorStore.GetCollection<string, MemoryRecord<string>>(req.CollectionName, recordDefinition);
                    if (failOnConflict)
                    {
                        await collection.CreateCollectionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await collection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                    }

                    break;
                }
                case InMemoryVectorStore:
                {
                    var collection = vectorStore.GetCollection<string, MemoryRecord<string>>(req.CollectionName, recordDefinition);

                    if (failOnConflict)
                    {
                        this._log.LogDebug("Creating new collection {CollectionName} with failOnConflict", req.CollectionName);
                        bool bugFix = await collection.CollectionExistsAsync(cancellationToken).ConfigureAwait(false);
                        if (bugFix)
                        {
                            return Results.Conflict("The collection already exists");
                        }

                        await collection.CreateCollectionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        this._log.LogDebug("Creating new collection {CollectionName} without failOnConflict", req.CollectionName);
                        try
                        {
                            await collection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (VectorStoreOperationException e) when (e.Message.Contains("Collection already exists", StringComparison.OrdinalIgnoreCase))
                        {
                            // Ignore SK bug, see https://github.com/microsoft/semantic-kernel/pull/11243
                        }
                    }

                    break;
                }
            }
        }
        catch (VectorStoreOperationException e)
        {
            // Qdrant + Azure AI Search
            if (e.InnerException?.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true)
            {
                this._log.LogError(e, "Unable to create {StorageType} collection, it already exists [1]", req.StorageType);
                return Results.Conflict($"{e.InnerException?.GetType().FullName}: {e.InnerException?.Message}");
            }

            if (e.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                this._log.LogError(e, "Unable to create {StorageType} collection, it already exists [2]", req.StorageType);
                return Results.Conflict($"{e.GetType().FullName}: {e.Message}");
            }

            this._log.LogError(e, "Unable to create {StorageType} collection, unexpected error: {Message}", req.StorageType, e.Message);
            return Results.InternalServerError(e.Message);
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Unable to create {StorageType} collection, unexpected error: {Message}", req.StorageType, e.Message);
            return Results.InternalServerError(e.Message);
        }

        this._log.LogDebug("New collection {CollectionName} created", req.CollectionName);

        return Results.Ok(new CreateCollectionResponse
        {
            Name = req.CollectionName,
            Type = req.StorageType,
            Message = $"{req.StorageType:G} collection created",
        });
    }
}
