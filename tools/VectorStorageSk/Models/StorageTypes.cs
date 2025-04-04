// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum StorageTypes
{
    Undefined = 0,
    AzureAISearch,
    AzureCosmosDbMongoDB,
    AzureCosmosDbNoSQL,
    Chroma,
    DuckDb,
    InMemory,
    Milvus,
    MongoDB,
    Pinecone,
    Postgres,
    Qdrant,
    Redis,
    SqlLite,
    SqlServer,
    Weaviate,
}
