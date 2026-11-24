namespace ApiIncidentFastTrack.DTOs.Incidents;

// One changed field. Rows saved together share a ChangeSetId (one edit = one change set).
// Values are text: enums by name, dates ISO 8601, amounts with "." (culture-independent),
// store/location/resolution type as their ids.
public class IncidentHistoryResponseDto
{
    public int HistoryId { get; set; }
    public Guid ChangeSetId { get; set; }

    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public int ChangedByUserId { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
