// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using VectorStorageSk.Models;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Functions;

internal sealed class UpsertRecordFunction
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<UpsertRecordFunction> _log;

    public UpsertRecordFunction(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._sp = sp;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<UpsertRecordFunction>();
    }

    public async Task<IResult> InvokeAsync(UpsertRecordRequest req, CancellationToken cancellationToken = default)
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

        var recordsByGuid = new List<GuidKeyedMemoryModel>();
        var recordsByString = new List<StringKeyedMemoryModel>();
        foreach (JsonElement value in req.Values)
        {
            Dictionary<string, JsonElement>? fieldValues = value.Deserialize<Dictionary<string, JsonElement>>();
            if (fieldValues == null) { return Results.InternalServerError("Values deserialization failed"); }

            Dictionary<string, object?> data = new();
            Dictionary<string, object?> vectors = new();

            var idFieldName = string.Empty;
            foreach (FieldDefinition field in req.Fields)
            {
                bool useDefaultValue = false;

                // Note: clients do/might not set primary keys, asking to auto-generate them
                if (!fieldValues.TryGetValue(field.Name, out JsonElement fieldValue))
                {
                    // Store the primary key field name, so that we can set it later
                    if (field.Type == FieldTypes.PrimaryKey)
                    {
                        idFieldName = field.Name;
                    }

                    // If a field is not set, we set a default value (below) and avoid null which cause to SQL exceptions
                    useDefaultValue = true;
                }

                switch (field.Type)
                {
                    case FieldTypes.PrimaryKey:
                        idFieldName = field.Name;
                        break;
                    case FieldTypes.Vector:
                        vectors[field.Name] = useDefaultValue ? new float[field.VectorSize ?? 1] : fieldValue.Deserialize<Embedding>();
                        break;
                    case FieldTypes.Text:
                        data[field.Name] = useDefaultValue ? string.Empty : fieldValue.Deserialize<string>();
                        break;
                    case FieldTypes.Bool:
                        try
                        {
                            data[field.Name] = useDefaultValue ? false : fieldValue.Deserialize<bool>();
                        }
                        catch (JsonException)
                        {
                            // Note: value validated earlier in the model
                            data[field.Name] = YamlExtensions.AsBoolean(fieldValue);
                        }

                        break;
                    case FieldTypes.Int:
                        data[field.Name] = useDefaultValue ? 0 : fieldValue.Deserialize<int>();
                        break;
                    case FieldTypes.Number:
                        data[field.Name] = useDefaultValue ? 0 : fieldValue.Deserialize<float>();
                        break;
                    case FieldTypes.DateTime:
                        data[field.Name] = useDefaultValue
                            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            : fieldValue.Deserialize<DateTimeOffset>().ToUnixTimeMilliseconds();
                        break;
                    case FieldTypes.Object:
                        data[field.Name] = useDefaultValue ? new object() : JsonSerializer.Serialize(fieldValue);
                        break;

                    case FieldTypes.ListOfNumber:
                        data[field.Name] = useDefaultValue ? [] : fieldValue.Deserialize<List<float>>();
                        break;
                    case FieldTypes.ListOfText:
                        data[field.Name] = useDefaultValue ? [] : fieldValue.Deserialize<List<string>>();
                        break;
                    case FieldTypes.ListOfBoolean:
                        data[field.Name] = useDefaultValue ? [] : fieldValue.Deserialize<List<bool>>();
                        break;

                    case FieldTypes.Undefined:
                    default:
                        // skip
                        break;
                }
            }

            // TODO: don't override primary keys if set by the client
            switch (vectorStore)
            {
                case QdrantVectorStore:
                {
                    // TODO: support ulong, check user request
                    var record = new GuidKeyedMemoryModel(Guid.NewGuid())
                    {
                        Data = data, Vectors = vectors
                    };
                    record.Data[idFieldName] = record.Key;
                    recordsByGuid.Add(record);
                    break;
                }
                case AzureAISearchVectorStore:
                case InMemoryVectorStore:
                case PostgresVectorStore:
                {
                    var record = new StringKeyedMemoryModel(Guid.NewGuid().ToString("D"))
                    {
                        Data = data, Vectors = vectors
                    };
                    record.Data[idFieldName] = record.Key;
                    recordsByString.Add(record);
                    break;
                }

                default:
                    return Results.InternalServerError("Storage type not supported");
            }
        }

        VectorStoreRecordDefinition recordDefinition = StorageLib.PrepareRecordDefinition(req.Fields, vectorStore);
        switch (vectorStore)
        {
            case QdrantVectorStore:
            {
                // TODO: support ulong, check user request
                var collection = vectorStore.GetCollection<Guid, GuidKeyedMemoryModel>(req.CollectionName, recordDefinition);
                foreach (var rec in recordsByGuid)
                {
                    await collection.UpsertAsync(rec, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                break;
            }
            case AzureAISearchVectorStore:
            case InMemoryVectorStore:
            case PostgresVectorStore:
            {
                var collection = vectorStore.GetCollection<string, StringKeyedMemoryModel>(req.CollectionName, recordDefinition);
                foreach (var rec in recordsByString)
                {
                    await collection.UpsertAsync(rec, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                break;
            }

            default:
                return Results.InternalServerError("Storage type not supported");
        }

        return Results.Ok(new UpsertRecordResponse()
        {
            Message = $"{recordsByGuid.Count + recordsByString.Count} records written",
        });
    }
}
