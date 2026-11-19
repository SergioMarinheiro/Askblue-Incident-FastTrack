namespace ApiIncidentFastTrack.DTOs.Incidents;

// Result of a submit attempt:
// - Submitted = true:  the incident moved to AwaitingApproval (HTTP 200 with Incident).
// - Submitted = false: possible duplicates were found and the status did not change
//                      (HTTP 409 with PossibleDuplicates).
public class SubmitIncidentResultDto
{
    public bool Submitted { get; set; }
    public IncidentResponseDto? Incident { get; set; }
    public List<PossibleDuplicateDto> PossibleDuplicates { get; set; } = new();
}
