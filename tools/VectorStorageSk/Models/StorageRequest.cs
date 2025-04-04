// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Models;

internal abstract class StorageRequest : IValidatableObject
{
    /// <summary>
    /// Storage engine, e.g. Azure AI Search, Qdrant, Pinecone, etc.
    /// </summary>
    [JsonPropertyName("storageType")]
    public StorageTypes StorageType { get; set; } = StorageTypes.Undefined;

    /// <summary>
    /// Type of the data model key. It can be left empty for default behavior.
    /// </summary>
    [JsonPropertyName("primaryKeyType")]
    public PrimaryKeyTypes PrimaryKeyType { get; set; } = PrimaryKeyTypes.Default;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(StorageTypes), this.StorageType))
        {
            yield return new ValidationResult($"Unknown storage type {this.StorageType:G}", [nameof(this.StorageType)]);
        }

        if (!Enum.IsDefined(typeof(PrimaryKeyTypes), this.PrimaryKeyType))
        {
            yield return new ValidationResult($"Unknown key type {this.PrimaryKeyType:G}", [nameof(this.PrimaryKeyType)]);
        }

        if (this.StorageType == StorageTypes.Undefined)
        {
            yield return new ValidationResult("Storage type not defined", [nameof(this.StorageType)]);
        }

        if (!StorageLib.IsStorageSupported(this.StorageType))
        {
            yield return new ValidationResult($"Storage type {this.StorageType:G} not supported", [nameof(this.StorageType)]);
        }
    }
}
