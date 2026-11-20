namespace ApiIncidentFastTrack.Exceptions;

// 404. The record doesn't exist, or belongs to another company (the tenant filter hides it,
// and we don't reveal that it exists). The message is shown to the client.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
