namespace ApiIncidentFastTrack.Models;

// CreatedAt (from the base) is the upload time.
public class IncidentAttachment : BaseDomainModel
{
    public int IncidentId { get; set; }
    public int UploadedByUserId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}