namespace ApiIncidentFastTrack.Exceptions;

// 422. The request is well-formed but breaks a business rule
// (e.g. submitting an incident with required fields missing, rejecting without a reason).
// The message is shown to the client, so write it for the user.
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

// 409. A business rule that conflicts with the record's current state
// (e.g. approving an incident that isn't AwaitingApproval, an email already in use).
// A subtype, so code that catches BusinessRuleException still catches it.
public class ConflictException : BusinessRuleException
{
    public ConflictException(string message) : base(message)
    {
    }
}
