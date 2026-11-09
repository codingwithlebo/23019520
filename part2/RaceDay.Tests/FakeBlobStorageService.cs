using System.Collections.Concurrent;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>In-memory stand-in for Azure Blob Storage, so tests and CI need no Azure account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _blobs = new();

    public int Count => _blobs.Count;

    public Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        _blobs[blobName] = (buffer.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task<StoredBlob?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        StoredBlob? result = null;
        if (_blobs.TryGetValue(blobName, out var blob))
            result = new StoredBlob(new MemoryStream(blob.Data), blob.ContentType);
        return Task.FromResult(result);
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
