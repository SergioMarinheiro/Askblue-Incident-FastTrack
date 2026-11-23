namespace ApiIncidentFastTrack.Interfaces;

// Where attachment files physically live. The database keeps only the details (name, type,
// who, when) plus the storage key returned by SaveAsync.
// Today: LocalFileStorage (a folder on disk). Moving to e.g. Azure Blob means one new class
// and one line in Program.cs; nothing else knows where the bytes are.
public interface IFileStorage
{
    // Stores the content under a new random key and returns that key.
    // The original file name is never used on storage (see LocalFileStorage).
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);

    // Null when nothing is stored under the key (e.g. the file was removed by hand).
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    // Does nothing if the key doesn't exist.
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
