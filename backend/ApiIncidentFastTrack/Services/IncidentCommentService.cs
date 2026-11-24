using System.Linq.Expressions;
using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Comments;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Anyone who can see the incident can read its comments and add one, in any status (CLAUDE.md).
// IncidentComment has no tenant filter (child entity), so the incident is checked first through
// IncidentVisibility: another company's incident, or another Staff member's, is 404.
// Editing and deleting comments are out of scope for now.
// Send-back and confirm-duplicate explanations are saved as comments by IncidentService,
// so they appear in this list too.
public class IncidentCommentService : IIncidentCommentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public IncidentCommentService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Oldest first, so the conversation reads top to bottom.
    public async Task<IEnumerable<CommentResponseDto>> GetCommentsAsync(int incidentId, CancellationToken cancellationToken)
    {
        await EnsureIncidentVisibleAsync(incidentId, cancellationToken);

        return await _db.IncidentComments
            .AsNoTracking()
            .Where(c => c.IncidentId == incidentId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<CommentResponseDto> AddCommentAsync(int incidentId, CreateCommentRequestDto request, CancellationToken cancellationToken)
    {
        await EnsureIncidentVisibleAsync(incidentId, cancellationToken);

        var comment = new IncidentComment
        {
            IncidentId = incidentId,
            UserId = _currentUser.UserId,
            Content = request.Content.Trim()
        };

        _db.IncidentComments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        // Read back through the projection, so the response includes the author's name like GET does.
        return await _db.IncidentComments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(ToDtoExpression)
            .SingleAsync(cancellationToken);
    }

    private async Task EnsureIncidentVisibleAsync(int incidentId, CancellationToken cancellationToken)
    {
        var visible = await _db.Incidents
            .VisibleTo(_currentUser)
            .AnyAsync(i => i.Id == incidentId, cancellationToken);

        if (!visible)
        {
            throw new NotFoundException($"Incident {incidentId} was not found.");
        }
    }

    // A deleted author still shows their name: soft delete keeps the row (by design).
    private static readonly Expression<Func<IncidentComment, CommentResponseDto>> ToDtoExpression = c => new CommentResponseDto
    {
        IncidentCommentId = c.Id,
        IncidentId = c.IncidentId,
        UserId = c.UserId,
        UserName = c.User.FirstName + " " + c.User.LastName,
        Content = c.Content,
        CreatedAt = c.CreatedAt
    };
}
