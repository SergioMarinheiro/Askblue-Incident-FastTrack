using ApiIncidentFastTrack.DTOs.Attachments;

namespace ApiIncidentFastTrack.Interfaces;

public interface IIncidentAttachmentService
{
    Task<IEnumerable<AttachmentResponseDto>> GetAttachmentsAsync(int incidentId, CancellationToken cancellationToken);
    Task<AttachmentResponseDto> UploadAttachmentAsync(int incidentId, UploadAttachmentRequestDto request, CancellationToken cancellationToken);
    Task<AttachmentDownloadDto> DownloadAttachmentAsync(int incidentId, int attachmentId, CancellationToken cancellationToken);
    Task DeleteAttachmentAsync(int incidentId, int attachmentId, CancellationToken cancellationToken);
}
