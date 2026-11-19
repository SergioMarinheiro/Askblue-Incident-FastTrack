using ApiIncidentFastTrack.DTOs.Comments;

namespace ApiIncidentFastTrack.Interfaces;

public interface IIncidentCommentService
{
    Task<IEnumerable<CommentResponseDto>> GetCommentsAsync(int incidentId, CancellationToken cancellationToken);
    Task<CommentResponseDto> AddCommentAsync(int incidentId, CreateCommentRequestDto request, CancellationToken cancellationToken);
}
