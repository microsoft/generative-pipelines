// Copyright (c) Microsoft. All rights reserved.

namespace EmbeddingGenerator.Config;

internal abstract class AIModelConfig
{
    public int MaxDimensions { get; set; } = 1536;
    public bool SupportsCustomDimensions { get; set; } = false;
    public int MaxBatchSize { get; set; } = 1;
    public string Tokenizer { get; set; } = string.Empty; // Not used yet
    public int MaxInputTokens { get; set; } = 8191; // Not used yet
}
