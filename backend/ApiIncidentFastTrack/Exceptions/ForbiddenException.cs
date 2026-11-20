namespace ApiIncidentFastTrack.Exceptions;

// 403. The caller can see the record but may not do this to it
// (e.g. Staff editing an incident that isn't theirs, a Manager deleting an Admin,
// a non-SuperAdmin sending CompanyId). The message is shown to the client.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
