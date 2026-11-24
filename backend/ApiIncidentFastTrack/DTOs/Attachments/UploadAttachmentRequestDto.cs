using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Attachments;

// Sent as multipart/form-data with one field named "file".
// Any file type is accepted (decided 2026-11-23). The 20 MB limit and the empty-file check
// are in the service; the controller's request size limit stops anything much larger earlier.
public class UploadAttachmentRequestDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
