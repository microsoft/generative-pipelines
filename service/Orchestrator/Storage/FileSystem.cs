// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Storage;

internal sealed class FileSystem : IFileSystem
{
    public string CombinePath(string path1, string path2)
    {
        return Path.Combine(path1, path2);
    }

    public Task CreateDirectoryIfNotExistsAsync(string path, CancellationToken ct = default)
    {
        if (Directory.Exists(path)) { return Task.CompletedTask; }

        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(Directory.Exists(path));
    }

    public Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task WriteAllTextAsync(string filename, string content, CancellationToken ct = default)
    {
        return File.WriteAllTextAsync(filename, content, ct);
    }

    public Task<string> ReadAllTextAsync(string filename, CancellationToken ct = default)
    {
        return File.ReadAllTextAsync(filename, ct);
    }
}
