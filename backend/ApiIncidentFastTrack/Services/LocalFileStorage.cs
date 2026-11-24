using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Stores each file in one folder, named by a random GUID (no extension), never by the
// uploaded name. That rules out:
// - path tricks ("..\..\appsettings.json" writing outside the folder),
// - two uploads called "photo.jpg" overwriting each other,
// - names the operating system refuses (":", "?", very long names),
// - guessing another file's name, or learning anything about the customer from it.
// The original name lives only in the database and is sent back on download.
public sealed class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;

    private readonly string _rootPath;

    // rootPath must be absolute; Program.cs resolves it against the content root.
    public LocalFileStorage(string rootPath)
    {
        if (!Path.IsPathFullyQualified(rootPath))
        {
            throw new ArgumentException("The attachment folder must be an absolute path.", nameof(rootPath));
        }

        _rootPath = rootPath;
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        var storageKey = Guid.NewGuid().ToString("N");
        var path = PathFor(storageKey);

        // CreateNew: never overwrite an existing file, even in the (practically impossible) case of a repeated GUID.
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);

        try
        {
            await using (file)
            {
                await content.CopyToAsync(file, cancellationToken);
            }
        }
        catch
        {
            // Don't leave half-written files behind (e.g. the upload was canceled).
            File.Delete(path);
            throw;
        }

        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            Stream stream = new FileStream(PathFor(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        // File.Delete doesn't throw when the file doesn't exist.
        File.Delete(PathFor(storageKey));
        return Task.CompletedTask;
    }

    // Keys come from our own database, but only accept the exact format we create:
    // a 32-character GUID can't contain a path separator or "..".
    private string PathFor(string storageKey)
    {
        if (!Guid.TryParseExact(storageKey, "N", out _))
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        return Path.Combine(_rootPath, storageKey);
    }
}
