// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Storage;

internal interface IFileSystem
{
    public string CombinePath(string path1, string path2);
    public Task CreateDirectoryIfNotExistsAsync(string path, CancellationToken ct = default);
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);
    public Task CreateDirectoryAsync(string path, CancellationToken ct = default);
    public Task WriteAllTextAsync(string filename, string content, CancellationToken ct = default);
    public Task<string> ReadAllTextAsync(string filename, CancellationToken ct = default);
}
