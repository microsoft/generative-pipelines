// Copyright (c) Microsoft. All rights reserved.

using VectorStorageSk.Models;
using VectorStorageSk.Storage;

namespace VectorStorageSk.Functions;

internal sealed class DefinitionsFunction
{
    public IResult Invoke()
    {
        return Results.Ok(
            new Dictionary<string, List<string>>()
            {
                // Supported stored engines, e.g. Postgres, Qdrant, etc.
                {
                    "StorageTypes", Enum.GetValues(typeof(StorageTypes))
                        .Cast<StorageTypes>()
                        .Where(type => type != StorageTypes.Undefined)
                        .Where(StorageLib.IsStorageSupported)
                        .Select(st => st.ToString("G"))
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList()
                },
                // Data model types available for read/write/search
                {
                    "DataTypes", ["MemoryRecord"]
                },
                // Field types supported for the data model fields
                {
                    "FieldTypes", Enum.GetValues(typeof(FieldTypes))
                        .Cast<FieldTypes>()
                        .Where(type => type != FieldTypes.Undefined)
                        .Select(st => st.ToString("G"))
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList()
                },
                // Data model primary key types
                {
                    "PrimaryKeyTypes", Enum.GetValues(typeof(PrimaryKeyTypes))
                        .Cast<PrimaryKeyTypes>()
                        .Select(st => st.ToString("G"))
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList()
                },
                // Properties of the data model fields
                {
                    "FieldProperties", [
                        "name",
                        "type",
                        "isFilterable",
                        "isFullTextSearchable",
                        "vectorSize",
                        "vectorDistance"
                    ]
                },
                // Supported vector distance functions
                {
                    "VectorDistanceFunctions", Enum.GetValues(typeof(VectorDistanceFunctions))
                        .Cast<VectorDistanceFunctions>()
                        .Where(type => type != VectorDistanceFunctions.Undefined)
                        .Select(st => st.ToString("G"))
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList()
                },
            }
        );
    }
}
