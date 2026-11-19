using ApiIncidentFastTrack.DTOs.Comments;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Anyone who can see the incident can read and add comments.
[ApiController]
[Route("api/incidents/{incidentId:int}/comments")]
public class IncidentCommentController : ControllerBase
{
    private readonly IIncidentCommentService _commentService;

    public IncidentCommentController(IIncidentCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(int incidentId, CancellationToken cancellationToken)
    {
        var comments = await _commentService.GetCommentsAsync(incidentId, cancellationToken);

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int incidentId, [FromBody] CreateCommentRequestDto request, CancellationToken cancellationToken)
    {
        var comment = await _commentService.AddCommentAsync(incidentId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetComments),
            new { incidentId },
            comment
        );
    }
}
