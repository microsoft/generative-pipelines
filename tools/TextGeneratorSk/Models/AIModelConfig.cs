// Copyright (c) Microsoft. All rights reserved.

namespace TextGeneratorSk.Models;

internal abstract class AIModelConfig
{
    public long ContextWindow { get; set; } = 16384;
    public long MaxOutputTokens { get; set; } = 16384;
}
