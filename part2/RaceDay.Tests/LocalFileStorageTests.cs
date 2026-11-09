using RaceDay.Api.Services;

namespace RaceDay.Tests;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"raceday-tests-{Guid.NewGuid():N}");

    private LocalFileStorageService CreateService() => new(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Upload_ThenDownload_ReturnsTheSameBytesAndContentType()
    {
        var service = CreateService();
        var bytes = new byte[] { 137, 80, 78, 71, 1, 2, 3 };

        await service.UploadAsync("events/1/banner.png", new MemoryStream(bytes), "image/png");
        var stored = await service.DownloadAsync("events/1/banner.png");

        Assert.NotNull(stored);
        await using var content = stored!.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        Assert.Equal(bytes, buffer.ToArray());
        Assert.Equal("image/png", stored.ContentType);
    }

    [Fact]
    public async Task Download_OfMissingFile_ReturnsNull()
    {
        var stored = await CreateService().DownloadAsync("events/99/missing.png");

        Assert.Null(stored);
    }

    [Fact]
    public async Task Delete_RemovesTheFile()
    {
        var service = CreateService();
        await service.UploadAsync("profiles/2/me.jpg", new MemoryStream(new byte[] { 1, 2, 3 }), "image/jpeg");

        await service.DeleteAsync("profiles/2/me.jpg");

        Assert.Null(await service.DownloadAsync("profiles/2/me.jpg"));
    }

    [Fact]
    public async Task Delete_OfMissingFile_DoesNotThrow()
    {
        await CreateService().DeleteAsync("events/5/nothing.png");
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("events/../../outside.png")]
    public async Task NamesThatEscapeTheStorageFolder_AreRejected(string name)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(name, new MemoryStream(new byte[] { 1 }), "image/png"));
    }
}
