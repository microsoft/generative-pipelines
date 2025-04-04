// Copyright (c) Microsoft. All rights reserved.

namespace Extractor.Models;

internal sealed class FileToProcess
{
    public BinaryData Content { get; set; } = new([]);
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; } = 0;
}
