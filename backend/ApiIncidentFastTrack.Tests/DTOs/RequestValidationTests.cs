using System.ComponentModel.DataAnnotations;
using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Tests.DTOs;

// Runs the same DataAnnotations check that [ApiController] runs before an action (400 when it fails).
public class RequestValidationTests
{
    private static List<string> InvalidMembers(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        return results.SelectMany(r => r.MemberNames).ToList();
    }

    private static CreateUserRequestDto ValidUser() => new()
    {
        FirstName = "Ana",
        LastName = "Silva",
        Email = "ana@example.com",
        Password = "secret",
        Role = UserRole.Staff
    };

    [Fact]
    public void DraftIncident_WithOnlyCustomerName_IsValid()
    {
        Assert.Empty(InvalidMembers(new CreateIncidentRequestDto { CustomerName = "John Doe" }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Incident_WithBlankCustomerName_IsInvalid(string customerName)
    {
        Assert.Contains(nameof(CreateIncidentRequestDto.CustomerName),
            InvalidMembers(new CreateIncidentRequestDto { CustomerName = customerName }));
    }

    [Fact]
    public void Incident_WithCustomerNameLongerThanTheColumn_IsInvalid()
    {
        Assert.Contains(nameof(CreateIncidentRequestDto.CustomerName),
            InvalidMembers(new CreateIncidentRequestDto { CustomerName = new string('x', 201) }));
    }

    [Fact]
    public void Incident_WithUndefinedEnumValue_IsInvalid()
    {
        var dto = new CreateIncidentRequestDto { CustomerName = "John Doe", Priority = (IncidentPriority)999 };

        Assert.Contains(nameof(CreateIncidentRequestDto.Priority), InvalidMembers(dto));
    }

    [Fact]
    public void Incident_WithNegativeCompensation_IsInvalid()
    {
        var dto = new CreateIncidentRequestDto { CustomerName = "John Doe", EstimatedCompensationAmount = -1m };

        Assert.Contains(nameof(CreateIncidentRequestDto.EstimatedCompensationAmount), InvalidMembers(dto));
    }

    [Fact]
    public void User_Valid_HasNoErrors()
    {
        Assert.Empty(InvalidMembers(ValidUser()));
    }

    [Fact]
    public void User_WithoutRole_IsInvalid()
    {
        // A missing "role" in the JSON arrives as 0, which is not a defined role.
        var dto = ValidUser();
        dto.Role = default;

        Assert.Contains(nameof(CreateUserRequestDto.Role), InvalidMembers(dto));
    }

    [Fact]
    public void User_WithInvalidEmail_IsInvalid()
    {
        var dto = ValidUser();
        dto.Email = "not-an-email";

        Assert.Contains(nameof(CreateUserRequestDto.Email), InvalidMembers(dto));
    }
}
