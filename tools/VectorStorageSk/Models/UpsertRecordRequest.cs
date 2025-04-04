// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class UpsertRecordRequest : CollectionRequest, IValidatableObject
{
    [JsonPropertyName("fields")]
    public List<FieldDefinition> Fields { get; set; } = new();

    [JsonPropertyName("values")]
    public List<object> Values { get; set; } = new();

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (this.Fields.Count > 0)
        {
            foreach (var result in this.Fields.SelectMany(f => f.Validate(validationContext)))
            {
                yield return result;
            }
        }

        for (int index = 0; index < this.Values.Count; index++)
        {
            // Check that each value is serialized correctly
            JsonElement value = (JsonElement)this.Values[index];
            Dictionary<string, JsonElement>? fieldValues = value.Deserialize<Dictionary<string, JsonElement>>();
            if (fieldValues == null)
            {
                yield return new ValidationResult($"Failed to deserialize value {index + 1} of {this.Values.Count}", [nameof(this.Values)]);
            }
            else
            {
                // Check boolean values syntax, needed to support YAML input
                foreach (FieldDefinition field in this.Fields)
                {
                    if (field.Type != FieldTypes.Bool) { continue; }

                    // Skip fields that are not set
                    if (!fieldValues.TryGetValue(field.Name, out JsonElement fieldValue)) { continue; }

                    bool? booleanValue;
                    try
                    {
                        _ = fieldValue.Deserialize<bool>();
                        continue;
                    }
                    catch (JsonException)
                    {
                        booleanValue = YamlExtensions.AsBoolean(fieldValue) == null;
                    }

                    if (!booleanValue.HasValue)
                    {
                        yield return new ValidationResult($"Invalid boolean value for {field.Name}: {fieldValue}");
                    }
                }
            }
        }
    }
}
