namespace ApiIncidentFastTrack.Enums;

public enum IncidentStatus
{
    New=1,    
    AwaitingApproval=2,
    AwaitingPayment = 3,
    AwaitingResolution = 4,
    Completed = 5,
    Rejected = 6,
    SentBack=7,
    Canceled = 8,
    Duplicate = 9
}