namespace ApiIncidentFastTrack.DTOs.Attachments;

// What the service hands the controller for a download. Not serialized to JSON:
// the controller streams Content as the response body. The controller's FileStreamResult
// disposes the stream once it has been sent.
public class AttachmentDownloadDto
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
}
