using ApiIncidentFastTrack.Services;

namespace ApiIncidentFastTrack.Tests.Services;

// Uses a fresh temporary folder per test (no database), removed afterwards.
public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "IncidentFastTrackTests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static MemoryStream Content(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheSameContent()
    {
        var key = await _storage.SaveAsync(Content("hello"), CancellationToken.None);

        await using var stream = await _storage.OpenReadAsync(key, CancellationToken.None);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        Assert.Equal("hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Save_StoresTheFileUnderARandomGuidName()
    {
        var key = await _storage.SaveAsync(Content("x"), CancellationToken.None);

        Assert.True(Guid.TryParseExact(key, "N", out _));
        Assert.Equal([Path.Combine(_root, key)], Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Save_TwiceWithTheSameContent_GivesTwoFiles()
    {
        var first = await _storage.SaveAsync(Content("same"), CancellationToken.None);
        var second = await _storage.SaveAsync(Content("same"), CancellationToken.None);

        Assert.NotEqual(first, second);
        Assert.Equal(2, Directory.GetFiles(_root).Length);
    }

    [Fact]
    public async Task Delete_RemovesTheFile()
    {
        var key = await _storage.SaveAsync(Content("x"), CancellationToken.None);

        await _storage.DeleteAsync(key, CancellationToken.None);

        Assert.Null(await _storage.OpenReadAsync(key, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_OfAMissingFile_DoesNotThrow()
    {
        Directory.CreateDirectory(_root);

        await _storage.DeleteAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);
    }

    [Fact]
    public async Task Open_OfAMissingFile_ReturnsNull()
    {
        Assert.Null(await _storage.OpenReadAsync(Guid.NewGuid().ToString("N"), CancellationToken.None));
    }

    [Theory]
    [InlineData(@"..\..\appsettings.json")]
    [InlineData("../secret.txt")]
    [InlineData("photo.jpg")]
    public async Task KeysThatAreNotOurGuids_AreRefused(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenReadAsync(key, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.DeleteAsync(key, CancellationToken.None));
    }

    [Fact]
    public void RelativeRootPath_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new LocalFileStorage("App_Data/attachments"));
    }
}
