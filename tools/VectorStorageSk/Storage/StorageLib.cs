// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using VectorStorageSk.Models;

namespace VectorStorageSk.Storage;

internal sealed class StorageLib
{
    public static bool IsStorageSupported(StorageTypes storageType)
    {
        return storageType switch
        {
            StorageTypes.AzureAISearch => true,
            StorageTypes.Qdrant => true,
            StorageTypes.InMemory => true,
            StorageTypes.Postgres => true,

            StorageTypes.AzureCosmosDbMongoDB => false,
            StorageTypes.AzureCosmosDbNoSQL => false,
            StorageTypes.Chroma => false, // not supported by SK
            StorageTypes.DuckDb => false, // not supported by SK
            StorageTypes.Milvus => false, // not supported by SK
            StorageTypes.MongoDB => false,
            StorageTypes.Pinecone => false,
            StorageTypes.Redis => false,
            StorageTypes.SqlLite => false,
            StorageTypes.SqlServer => false, // not supported by SK
            StorageTypes.Weaviate => false,

            _ => throw new ArgumentOutOfRangeException(nameof(storageType), storageType, "Unexpected storage type")
        };
    }

    public static (IVectorStore? vectorStore, IResult? err) GetVectorStore(StorageTypes storageType, IServiceProvider sp, ILogger log)
    {
        IVectorStore vectorStore;

        switch (storageType)
        {
            case StorageTypes.AzureAISearch:
            {
                vectorStore = sp.GetRequiredService<AzureAISearchVectorStore>();
                break;
            }
            case StorageTypes.InMemory:
            {
                vectorStore = sp.GetRequiredService<InMemoryVectorStore>();
                break;
            }
            case StorageTypes.Qdrant:
            {
                vectorStore = sp.GetRequiredService<QdrantVectorStore>();
                break;
            }
            case StorageTypes.Postgres:
            {
                vectorStore = sp.GetRequiredService<PostgresVectorStore>();
                break;
            }

            case StorageTypes.Undefined:
                log.LogError("Storage type node defined");
                return (null, Results.BadRequest("Storage type not defined"));

            default:
                log.LogError("Unknown storage type {StorageType}", storageType.ToString("G"));
                return (null, Results.BadRequest("Unknown storage type"));

            // case StorageTypes.AzureCosmosDbMongoDB: var test = new AzureCosmosDBMongoDBVectorStore();
            // case StorageTypes.AzureCosmosDbNoSQL: var test = new AzureCosmosDBNoSQLVectorStore();
            // case StorageTypes.MongoDB: var test = new MongoDBVectorStore();
            // case StorageTypes.Pinecone: var test = new PineconeVectorStore();
            // case StorageTypes.Postgres: var test = new PostgresVectorStore();
            // case StorageTypes.Redis: var test = new RedisVectorStore();
            // case StorageTypes.SqlLite: var test = new SqliteVectorStore();
            // case StorageTypes.Weaviate: var test = new WeaviateVectorStore();
        }

        if (vectorStore != null) { return (vectorStore, null); }

        log.LogError("Unable to instantiate {VectorStoreType} client", storageType.ToString("G"));
        return (null, Results.InternalServerError($"Unable to instantiate {storageType:G} client"));
    }

    public static VectorStoreRecordDefinition PrepareRecordDefinition(List<FieldDefinition> fields, IVectorStore vectorStore)
    {
        if (fields == null || fields.Count == 0)
        {
            throw new ArgumentException("List of fields empty or not defined", nameof(fields));
        }

        // Missing field name
        var field = fields.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.Name));
        if (field != null) { throw new ArgumentException($"Field name cannot be empty", nameof(fields)); }

        // Missing field type
        field = fields.FirstOrDefault(f => f.Type == FieldTypes.Undefined);
        if (field != null) { throw new ArgumentException($"Field '{field.Name}' type is not defined"); }

        // Number of primary keys
        int pkCount = fields.Count(f => f.Type == FieldTypes.PrimaryKey);
        switch (pkCount)
        {
            case 0: throw new ArgumentException($"Primary key not defined. Add a field of type '{nameof(FieldTypes.PrimaryKey)}'", nameof(fields));
            case > 1: throw new ArgumentException("Only one primary key is allowed.");
        }

        // Missing vector size
        field = fields.FirstOrDefault(f => f is { Type: FieldTypes.Vector, VectorSize: null });
        if (field != null) { throw new ArgumentException($"Vector '{field.Name}' size not defined. Set the 'vectorSize' property."); }

        // Vector size must be greater than 0
        field = fields.FirstOrDefault(f => f is { Type: FieldTypes.Vector, VectorSize: < 1 });
        if (field != null) { throw new ArgumentException($"Vector '{field.Name}' size must be greater than 0"); }

        var properties = new List<VectorStoreRecordProperty>();
        foreach (var f in fields)
        {
            switch (f.Type)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(f.Type), f.Type, $"{f.Name} field type unknown or not supported");

                case FieldTypes.PrimaryKey:
                    // More conditions needed here to support other storage engines (ulong keys, long, int, composite keys, etc.)
                    properties.Add(vectorStore is QdrantVectorStore
                        ? new VectorStoreRecordKeyProperty(f.Name, typeof(Guid))
                        : new VectorStoreRecordKeyProperty(f.Name, typeof(string)));

                    break;
                case FieldTypes.Vector:
                    properties.Add(new VectorStoreRecordVectorProperty(f.Name, typeof(Embedding))
                    {
                        Dimensions = f.VectorSize!.Value,
                        DistanceFunction = f.VectorDistanceFunction.ToString(), //DistanceFunction.CosineSimilarity,
                        IndexKind = IndexKind.Hnsw // TODO: depends on storage type
                    });
                    break;
                case FieldTypes.Text:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(string))
                    {
                        IsFilterable = f.IsFilterable,
                        IsFullTextSearchable = f.IsFullTextSearchable
                    });
                    break;
                case FieldTypes.Bool:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(bool))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.Int:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(long))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.Number:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(double))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.DateTime:
                    // SK missing date time support for Qdrant
                    // properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(DateTimeOffset))
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(long))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.Object:
                    // Note: using JSON serialization
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(string))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.ListOfNumber:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(List<long>))
                    {
                        IsFilterable = f.IsFilterable,
                    });
                    break;
                case FieldTypes.ListOfText:
                    properties.Add(new VectorStoreRecordDataProperty(f.Name, typeof(List<string>))
                    {
                        IsFilterable = f.IsFilterable,
                        IsFullTextSearchable = f.IsFullTextSearchable
                    });
                    break;
            }
        }

        return new VectorStoreRecordDefinition { Properties = properties };
    }
}
