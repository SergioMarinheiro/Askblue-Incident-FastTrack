using ApiIncidentFastTrack.DTOs.Comments;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class IncidentCommentService : IIncidentCommentService
{
    public Task<IEnumerable<CommentResponseDto>> GetCommentsAsync(int incidentId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<CommentResponseDto> AddCommentAsync(int incidentId, CreateCommentRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
