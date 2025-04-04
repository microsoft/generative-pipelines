// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CommonDotNet.Models;

namespace EmbeddingGenerator.Models;

internal class EmbeddingRequest : IValidatable<EmbeddingRequest>
{
    [JsonPropertyName("modelId")]
    [JsonPropertyOrder(1)]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Input { get; set; }

    [JsonPropertyName("inputs")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Inputs { get; set; }

    [JsonPropertyName("dimensions")]
    [JsonPropertyOrder(13)]
    public int? Dimensions { get; set; }

    [JsonPropertyName("cache")]
    [JsonPropertyOrder(30)]
    public bool Cache { get; set; } = true;

    [JsonPropertyName("ignoreCache")]
    [JsonPropertyOrder(31)]
    public bool IgnoreCache { get; set; } = false;

    public EmbeddingRequest FixState()
    {
        if (string.IsNullOrWhiteSpace(this.ModelId)) { this.ModelId = string.Empty; }

        this.ModelId = this.ModelId.Trim();

        if (this.Dimensions < 0) { this.Dimensions = null; }

        return this;
    }

    public bool IsValid(out string errMsg)
    {
        errMsg = "The request is not valid";
        if (string.IsNullOrWhiteSpace(this.ModelId))
        {
            errMsg = "The modelId is required, the value is empty";
            return false;
        }

        if ((this.Inputs == null || this.Inputs.Count == 0) && string.IsNullOrEmpty(this.Input))
        {
            errMsg = $"Both {nameof(this.Input)} and {nameof(this.Inputs)} are empty";
            return false;
        }

        if (this.Inputs?.Count > 0 && !string.IsNullOrEmpty(this.Input))
        {
            errMsg = $"Both {nameof(this.Input)} and {nameof(this.Inputs)} are provided, only one is allowed, specifying either a single value or a list of values";
            return false;
        }

        return true;
    }

    public EmbeddingRequest Validate()
    {
        if (!this.FixState().IsValid(out var errMsg)) { throw new ValidationException(errMsg); }

        return this;
    }
}
