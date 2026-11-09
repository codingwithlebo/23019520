namespace RaceDay.Api.Services;

/// <summary>
/// Stores images as files in a folder owned by the API (by default wwwroot/uploads).
/// It sits behind the same interface as Azure Blob Storage, so controllers, the MVC app and the
/// tests are unaffected, and the files are still only reachable through the API's endpoints.
/// </summary>
public class LocalFileStorageService(string rootPath) : IBlobStorageService
{
    private readonly string _root = Path.GetFullPath(rootPath);

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<StoredBlob?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(blobName);
        if (!File.Exists(path)) return Task.FromResult<StoredBlob?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = ImageUpload.ContentTypeFor(Path.GetExtension(path));
        return Task.FromResult<StoredBlob?>(new StoredBlob(stream, contentType));
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(blobName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Maps a blob name to a file path, refusing anything that would escape the storage folder.</summary>
    private string ResolvePath(string blobName)
    {
        var full = Path.GetFullPath(Path.Combine(_root, blobName));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Invalid file name.", nameof(blobName));
        return full;
    }
}
