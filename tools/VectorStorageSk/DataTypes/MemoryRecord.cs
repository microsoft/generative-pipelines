// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.VectorData;

namespace VectorStorageSk.SemanticKernel;

public class MemoryRecord<T>
{
    /// <summary>
    /// Unique record identifier.
    /// </summary>
    [Key]
    [VectorStoreRecordKey]
    public T Id { get; set; } = default!;

    /// <summary>
    /// Text content that is being stored and indexed with the embedding.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector for the chunk of text.
    /// </summary>
    [VectorStoreRecordVector(5)]
    public Embedding ContentEmbedding { get; set; }

    /// <summary>
    /// Unique identifier for the source used to create this and related records.
    /// When uploading information, all the previous records with the same MemoryId
    /// should be deleted to avoid duplicates.
    /// It's like a special tag, only stored in a dedicated field.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true)]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// A list of strings that can be used to categorize the record.
    /// For example, "type:news", "user:213", "env:prod", etc.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true)]
    public List<string> Tags { get; set; } = [];

    // ================ GENERICALLY USEFUL FIELDS ====================

    /// <summary>
    /// Used to indicate that the record is a test record, e.g. for testing purposes.
    /// It's like a special tag, only stored in a dedicated field.
    /// The use case is not defined, use it if you need it.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true)]
    public bool IsTest { get; set; } = false;

    /// <summary>
    /// Optional title for the record.
    /// The use case is not defined, use it if you need it.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL/URI/ID pointer to the source of the record.
    /// The use case is not defined, use it if you need it.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true)]
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Optional timestamp for the record.
    /// SK is missing DateTime for Qdrant, so for now this is a long.
    /// The use case is not defined, use it if you need it.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = true)]
    public long TimeStamp { get; set; }

    /// <summary>
    /// Optional field for storing any other information, e.g. JSON serialized data.
    /// The use case is not defined, use it if you need it.
    /// </summary>
    [VectorStoreRecordData(IsFilterable = false)]
    public string Other { get; set; } = string.Empty;
}
