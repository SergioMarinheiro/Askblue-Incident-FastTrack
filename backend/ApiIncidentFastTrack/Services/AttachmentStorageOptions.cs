namespace ApiIncidentFastTrack.Services;

// "AttachmentStorage" section in appsettings.json.
public class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";

    // Decided 2026-11-23: 20 MB per file, no restriction on file type.
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;

    // Folder for the stored files. A relative path is resolved against the content root
    // (the project folder when running locally). Keep it outside wwwroot: files are only
    // ever served through the API, which checks who is asking.
    public string RootPath { get; set; } = "App_Data/attachments";
}
