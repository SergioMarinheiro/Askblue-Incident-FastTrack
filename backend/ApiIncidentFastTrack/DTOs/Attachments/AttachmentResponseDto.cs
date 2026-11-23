namespace ApiIncidentFastTrack.DTOs.Attachments;

// The file's details, without the file itself (download it with GET .../attachments/{id}).
// The storage key is internal and never returned.
public class AttachmentResponseDto
{
    public int IncidentAttachmentId { get; set; }
    public int IncidentId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
