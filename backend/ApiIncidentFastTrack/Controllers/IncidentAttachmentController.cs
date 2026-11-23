using ApiIncidentFastTrack.DTOs.Attachments;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Anyone who can see the incident can list, download, upload and delete its files.
// Upload/delete only while New, AwaitingApproval or SentBack (checked in the service).
[ApiController]
[Route("api/incidents/{incidentId:int}/attachments")]
public class IncidentAttachmentController : ControllerBase
{
    // The 20 MB file plus room for the multipart framing. Larger requests are refused by the
    // server while reading (400) before the file is fully received; between 20 MB and this
    // limit the service answers with a clearer 422.
    private const long MaxRequestBytes = AttachmentStorageOptions.MaxFileSizeBytes + 1024 * 1024;

    private readonly IIncidentAttachmentService _attachmentService;

    public IncidentAttachmentController(IIncidentAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAttachments(int incidentId, CancellationToken cancellationToken)
    {
        var attachments = await _attachmentService.GetAttachmentsAsync(incidentId, cancellationToken);

        return Ok(attachments);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<AttachmentResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAttachment(int incidentId, [FromForm] UploadAttachmentRequestDto request, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentService.UploadAttachmentAsync(incidentId, request, cancellationToken);

        return CreatedAtAction(
            nameof(DownloadAttachment),
            new { incidentId, attachmentId = attachment.IncidentAttachmentId },
            attachment
        );
    }

    // Always a download, never shown inside the browser, whatever the file is (an .html or .svg
    // could otherwise run scripts with the API's origin):
    // - File(..., fileDownloadName) sends "Content-Disposition: attachment".
    // - The type is always application/octet-stream, not what the uploader declared.
    // - nosniff stops the browser from guessing a type from the content.
    [HttpGet("{attachmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAttachment(int incidentId, int attachmentId, CancellationToken cancellationToken)
    {
        var download = await _attachmentService.DownloadAttachmentAsync(incidentId, attachmentId, cancellationToken);

        Response.Headers.XContentTypeOptions = "nosniff";

        return File(download.Content, "application/octet-stream", download.FileName);
    }

    [HttpDelete("{attachmentId:int}")]
    public async Task<IActionResult> DeleteAttachment(int incidentId, int attachmentId, CancellationToken cancellationToken)
    {
        await _attachmentService.DeleteAttachmentAsync(incidentId, attachmentId, cancellationToken);

        return NoContent();
    }
}
