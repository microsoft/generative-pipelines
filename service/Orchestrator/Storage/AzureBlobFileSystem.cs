// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Config;
using Orchestrator.Diagnostics;

namespace Orchestrator.Storage;

internal sealed class AzureBlobFileSystem : IFileSystem
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobFileSystem> _log;

    private readonly string _containerName;
    private bool _containerExists = false;
    private readonly bool _leaseBlobs;

    //[FromKeyedServices("myKey")] BlobServiceClient client
    public AzureBlobFileSystem(
        BlobServiceClient serviceClient,
        AzureBlobFileSystemConfig config,
        ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AzureBlobFileSystem>();
        this._containerName = config.Container;
        this._leaseBlobs = config.LeaseBlobs;

        this._containerClient = serviceClient.GetBlobContainerClient(config.Container);
    }

    public string CombinePath(string path1, string path2)
    {
        return $"{path1}/{path2}";
    }

    public Task CreateDirectoryIfNotExistsAsync(string path, CancellationToken ct = default)
    {
        return this.CreateDirectoryAsync(path, ct);
    }

    public async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        // Note: Azure Blob storage doesn't have an artifact for "directories", which are just a detail
        //       in a blob name so there's no such thing as creating a directory. When creating a blob,
        //       the name must contain the directory name, e.g. blob.Name = "dir1/subdir2/file.txt"

        if (!this._containerExists)
        {
            this._log.LogTrace("Creating container (if not exists) '{ContainerName}' ...", this._containerName);

            // Note: _containerClient.CreateIfNotExistsAsync logs a warning if the container already exists
            //       which is a false positive and annoying. We call ExistsAsync() to avoid this.
            if (await this._containerClient.ExistsAsync(ct).ConfigureAwait(false)) { return; }

            await this._containerClient
                .CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct)
                .ConfigureAwait(false);

            this._log.LogTrace("Container '{ContainerName}' ready", this._containerName);
            this._containerExists = true;
        }
    }

    public async Task WriteAllTextAsync(string filename, string content, bool firstWrite, CancellationToken ct = default)
    {
        this._log.LogTrace("Writing blob {BlobName}, size {BlobSize} ...", filename, content.Length);

        BlobClient blobClient = this._containerClient.GetBlobClient(filename);
        BlobUploadOptions options = new() { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } };

        var blobLock = await this.LockAsync(blobClient, filename, firstWrite, ct).ConfigureAwait(false);
        try
        {
            if (blobLock.leaseId != null) { options.Conditions = new BlobRequestConditions { LeaseId = blobLock.leaseId }; }

            await blobClient.UploadAsync(BinaryData.FromString(content), options, ct).ConfigureAwait(false);
        }
        finally
        {
            await this.UnlockAsync(blobLock.leaseClient, blobLock.leaseId, ct).ConfigureAwait(false);
        }

        this._log.LogTrace("Blob {BlobName} ready", filename);
    }

    public async Task<string> ReadAllTextAsync(string filename, CancellationToken ct = default)
    {
        BlobClient blobClient = this._containerClient.GetBlobClient(filename);

        try
        {
            Response<BlobDownloadResult>? result = await blobClient.DownloadContentAsync(ct).ConfigureAwait(false);
            if (!result.HasValue) { throw new StorageException($"Unable to read blob {filename}"); }

            return result.Value.Content?.ToString() ?? string.Empty;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            throw new FileNotFoundException("Blob not found", filename);
        }
    }

    private async Task<(string? leaseId, BlobLeaseClient? leaseClient)> LockAsync(
        BlobClient blobClient, string filename, bool firstWrite, CancellationToken ct)
    {
        if (!this._leaseBlobs) { return (null, null); }

        this._log.LogTrace("Locking {BlobName} ...", filename);
        string? leaseId = null;
        BlobLeaseClient? blobLeaseClient = null;
        if (await blobClient.ExistsAsync(ct).ConfigureAwait(false))
        {
            if (firstWrite)
            {
                this._log.LogTrace("{BlobName} should not exist, locking as requested anyway...", filename);
            }

            blobLeaseClient = blobClient.GetBlobLeaseClient();
            var lease = await blobLeaseClient.AcquireAsync(TimeSpan.FromSeconds(60), cancellationToken: ct).ConfigureAwait(false);
            if (!lease.HasValue)
            {
                this._log.LogWarning("{BlobName} lock failed, lease is NULL", filename);
                throw new StorageException("Unable to lease blob");
            }

            leaseId = lease.Value.LeaseId;
            this._log.LogTrace("Locked {BlobName}, Lease {LeaseId}", filename, leaseId);
        }
        else
        {
            if (!firstWrite)
            {
                this._log.LogWarning("{BlobName} not found, cannot lock", filename, leaseId);
            }
            else
            {
                this._log.LogTrace("{BlobName} not found (as expected), nothing to lock", filename);
            }
        }

        return (leaseId, blobLeaseClient);
    }

    private async Task UnlockAsync(BlobLeaseClient? blobLeaseClient, string? leaseId, CancellationToken ct)
    {
        if (blobLeaseClient == null || leaseId == null) { return; }

        this._log.LogTrace("Unlocking {LeaseId}", leaseId);

        await blobLeaseClient
            .ReleaseAsync(new BlobRequestConditions { LeaseId = leaseId }, cancellationToken: ct)
            .ConfigureAwait(false);
    }
}
