namespace ApiIncidentFastTrack.Models;

public class IncidentAttachment
{
    public int IncidentAttachmentId { get; set; }

    public int IncidentId { get; set; }
    public int UploadedByUserId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}