// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace TextGenerator.Functions;

internal sealed class Report
{
    [JsonPropertyName("finishReason")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FinishReason { get; set; } = null;

    [JsonPropertyName("completionId")]
    [JsonPropertyOrder(15)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompletionId { get; set; } = null;

    [JsonPropertyName("totalTokenCount")]
    [JsonPropertyOrder(20)]
    public int TotalTokenCount { get; set; }

    [JsonPropertyName("inputTokenCount")]
    [JsonPropertyOrder(30)]
    public int InputTokenCount { get; set; }

    [JsonPropertyName("outputTokenCount")]
    [JsonPropertyOrder(40)]
    public int OutputTokenCount { get; set; }

    [JsonPropertyName("inputAudioTokenCount")]
    [JsonPropertyOrder(50)]
    public int InputAudioTokenCount { get; set; }

    [JsonPropertyName("inputCachedTokenCount")]
    [JsonPropertyOrder(60)]
    public int InputCachedTokenCount { get; set; }

    [JsonPropertyName("outputAudioTokenCount")]
    [JsonPropertyOrder(70)]
    public int OutputAudioTokenCount { get; set; }

    [JsonPropertyName("outputAcceptedPredictionTokenCount")]
    [JsonPropertyOrder(80)]
    public int OutputAcceptedPredictionTokenCount { get; set; }

    [JsonPropertyName("outputReasoningTokenCount")]
    [JsonPropertyOrder(90)]
    public int OutputReasoningTokenCount { get; set; }

    [JsonPropertyName("outputRejectedPredictionTokenCount")]
    [JsonPropertyOrder(100)]
    public int OutputRejectedPredictionTokenCount { get; set; }
}
