using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Companies;

public class CreateCompanyRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
