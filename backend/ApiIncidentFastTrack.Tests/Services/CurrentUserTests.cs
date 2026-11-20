using System.Security.Claims;
using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Services;
using Microsoft.AspNetCore.Http;

namespace ApiIncidentFastTrack.Tests.Services;

public class CurrentUserTests
{
    // authenticationType != null is what makes Identity.IsAuthenticated true,
    // just like the JwtBearer middleware does after validating a token.
    private static CurrentUser CreateCurrentUser(string? authenticationType, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType, AppClaimTypes.UserId, AppClaimTypes.Role);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        return new CurrentUser(accessor);
    }

    private static CurrentUser CreateAuthenticated(params Claim[] claims) => CreateCurrentUser("Test", claims);

    [Fact]
    public void StaffWithCompany_ExposesUserIdRoleAndCompanyId()
    {
        var currentUser = CreateAuthenticated(
            new Claim(AppClaimTypes.UserId, "42"),
            new Claim(AppClaimTypes.Role, "Staff"),
            new Claim(AppClaimTypes.CompanyId, "7"));

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(42, currentUser.UserId);
        Assert.Equal(UserRole.Staff, currentUser.Role);
        Assert.Equal(7, currentUser.CompanyId);
        Assert.False(currentUser.IsSuperAdmin);
    }

    [Fact]
    public void SuperAdminWithoutCompany_IsSuperAdminAndHasNoCompanyId()
    {
        var currentUser = CreateAuthenticated(
            new Claim(AppClaimTypes.UserId, "1"),
            new Claim(AppClaimTypes.Role, "SuperAdmin"));

        Assert.True(currentUser.IsSuperAdmin);
        Assert.Equal(UserRole.SuperAdmin, currentUser.Role);
        Assert.Null(currentUser.CompanyId);
    }

    [Fact]
    public void NoHttpContext_IsAnonymousAndDoesNotThrowForFilterValues()
    {
        // Outside a request: the seeder at startup, dotnet ef at design time.
        var currentUser = new CurrentUser(new HttpContextAccessor());

        Assert.False(currentUser.IsAuthenticated);
        Assert.False(currentUser.IsSuperAdmin);
        Assert.Null(currentUser.CompanyId);
        Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public void UnauthenticatedIdentity_ClaimsAreIgnored()
    {
        // Claims on an identity that was never authenticated must never count.
        var currentUser = CreateCurrentUser(
            authenticationType: null,
            new Claim(AppClaimTypes.UserId, "1"),
            new Claim(AppClaimTypes.Role, "SuperAdmin"),
            new Claim(AppClaimTypes.CompanyId, "7"));

        Assert.False(currentUser.IsAuthenticated);
        Assert.False(currentUser.IsSuperAdmin);
        Assert.Null(currentUser.CompanyId);
    }

    [Theory]
    [InlineData("1")]          // numeric value of SuperAdmin: Enum.TryParse would accept it
    [InlineData("superadmin")] // wrong case
    [InlineData("Owner")]      // not a role
    public void RoleClaimThatIsNotAnExactRoleName_IsRejected(string roleValue)
    {
        var currentUser = CreateAuthenticated(
            new Claim(AppClaimTypes.UserId, "42"),
            new Claim(AppClaimTypes.Role, roleValue));

        Assert.False(currentUser.IsSuperAdmin);
        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public void NonSuperAdminWithoutCompanyClaim_HasNoCompanyId()
    {
        // Fails closed: the tenant filter then matches nothing for this caller.
        var currentUser = CreateAuthenticated(
            new Claim(AppClaimTypes.UserId, "42"),
            new Claim(AppClaimTypes.Role, "Admin"));

        Assert.Null(currentUser.CompanyId);
        Assert.False(currentUser.IsSuperAdmin);
    }
}
