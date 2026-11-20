using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Comments;

public class CreateCommentRequestDto
{
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}
