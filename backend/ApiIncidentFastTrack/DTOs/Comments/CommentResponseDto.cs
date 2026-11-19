namespace ApiIncidentFastTrack.DTOs.Comments;

public class CommentResponseDto
{
    public int IncidentCommentId { get; set; }
    public int IncidentId { get; set; }

    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
