namespace ApiIncidentFastTrack.DTOs.Incidents;

public class SubmitIncidentRequestDto
{
    // False on the first submit. Set to true after the user has seen the
    // possible duplicates and chosen to proceed anyway.
    public bool ProceedDespiteDuplicates { get; set; }
}
