using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public record StoredBlob(Stream Content, string ContentType);

/// <summary>
/// The only place that talks to Azure Blob Storage. Controllers depend on this interface,
/// so tests can swap in a fake and the MVC app never needs to know Azure exists.
/// </summary>
public interface IBlobStorageService
{
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<StoredBlob?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}

public class BlobStorageNotConfiguredException() : Exception("Azure Blob Storage is not configured.");

public class AzureBlobStorageService(IConfiguration config) : IBlobStorageService
{
    private BlobContainerClient? _container;

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (_container is not null) return _container;

        var connectionString = config["AzureBlob:ConnectionString"];
        var containerName = config["AzureBlob:ContainerName"] ?? "raceday-images";
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new BlobStorageNotConfiguredException();

        var container = new BlobContainerClient(connectionString, containerName);
        // Private container: images are only ever served through the API.
        await container.CreateIfNotExistsAsync(publicAccessType: PublicAccessType.None, cancellationToken: cancellationToken);
        _container = container;
        return container;
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await container.GetBlobClient(blobName).UploadAsync(content, options, cancellationToken);
    }

    public async Task<StoredBlob?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        try
        {
            var response = await container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new StoredBlob(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
