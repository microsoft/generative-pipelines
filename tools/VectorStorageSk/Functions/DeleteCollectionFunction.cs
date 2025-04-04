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
internal sealed class DeleteCollectionFunction
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DeleteCollectionFunction> _log;

    public DeleteCollectionFunction(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._sp = sp;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<DeleteCollectionFunction>();
    }

    public async Task<IResult> InvokeAsync(DeleteCollectionRequest req, CancellationToken cancellationToken = default)
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

        try
        {
            switch (vectorStore)
            {
                case QdrantVectorStore:
                {
                    this._log.LogDebug("Preparing record definition (for collection deletion)");
                    VectorStoreRecordDefinition fakeRecordDefinition = new()
                    {
                        Properties =
                        [
                            new VectorStoreRecordKeyProperty("any1", typeof(Guid)),
                            new VectorStoreRecordVectorProperty("any2", typeof(Embedding))
                        ]
                    };
                    var collection = vectorStore.GetCollection<Guid, MemoryRecord<Guid>>(req.CollectionName, fakeRecordDefinition);
                    this._log.LogDebug("Deleting collection {CollectionName}", req.CollectionName);
                    await collection.DeleteCollectionAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }
                case PostgresVectorStore:
                case AzureAISearchVectorStore:
                {
                    this._log.LogDebug("Preparing collection definition (for deletion)");
                    VectorStoreRecordDefinition fakeRecordDefinition = new()
                    {
                        Properties =
                        [
                            new VectorStoreRecordKeyProperty("any", typeof(string)),
                            // new VectorStoreRecordVectorProperty("any2", typeof(Embedding))
                        ]
                    };
                    var collection = vectorStore.GetCollection<string, MemoryRecord<string>>(req.CollectionName, fakeRecordDefinition);
                    this._log.LogDebug("Deleting collection {CollectionName}", req.CollectionName);
                    await collection.DeleteCollectionAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }
                case InMemoryVectorStore:
                {
                    this._log.LogDebug("Preparing collection definition (for deletion)");
                    VectorStoreRecordDefinition fakeRecordDefinition = new()
                    {
                        Properties =
                        [
                            new VectorStoreRecordKeyProperty("any", typeof(string)),
                            // new VectorStoreRecordVectorProperty("any2", typeof(Embedding))
                        ]
                    };
                    var collection = vectorStore.GetCollection<string, MemoryRecord<string>>(req.CollectionName, fakeRecordDefinition);
                    this._log.LogDebug("Deleting collection {CollectionName}", req.CollectionName);
                    await collection.DeleteCollectionAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }

                default:
                    return Results.InternalServerError("Storage type not supported");
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ArgumentException)
        {
            this._log.LogError(e, "Unable to delete {StorageType} collection, unexpected error: {Message}", req.StorageType, e.Message);
            return Results.InternalServerError(e.Message);
        }
        catch (Exception e)
        {
            this._log.LogError(e, "FOO 1 Unable to delete {StorageType} collection, unexpected error: {Message}", req.StorageType, e.Message);
            this._log.LogError(e.InnerException, "FOO 2 Unable to delete {StorageType} collection, unexpected error: {Message}", req.StorageType, e.Message);
            return Results.InternalServerError(e.Message);
        }

        return Results.Ok(new DeleteCollectionResponse { });
    }
}
