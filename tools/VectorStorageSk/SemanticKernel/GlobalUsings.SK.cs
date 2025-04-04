// Copyright (c) Microsoft. All rights reserved.

// Simplify the usage of embeddings

global using Embedding = System.ReadOnlyMemory<float>;

// Simplify the usage of SK VectorStore data models
global using GuidKeyedMemoryModel = Microsoft.Extensions.VectorData.VectorStoreGenericDataModel<System.Guid>;
global using StringKeyedMemoryModel = Microsoft.Extensions.VectorData.VectorStoreGenericDataModel<string>;
