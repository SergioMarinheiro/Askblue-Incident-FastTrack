using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Tests.Data;

// Checks the SQL that EF generates. ToQueryString() never opens a connection,
// so no database is needed. This proves the filter is wired in, not how it behaves on real rows.
public class TenantFilterTests
{
    private sealed class FakeCurrentUser(bool isSuperAdmin, int? companyId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int UserId => 1;
        public UserRole Role => isSuperAdmin ? UserRole.SuperAdmin : UserRole.Staff;
        public int? CompanyId => companyId;
        public bool IsSuperAdmin => isSuperAdmin;
    }

    private static AppDbContext CreateContext(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused") // never connected to
            .Options;

        return new AppDbContext(options, currentUser);
    }

    [Fact]
    public void TenantOwnedEntities_AreFilteredByCompanyId()
    {
        using var db = CreateContext(new FakeCurrentUser(isSuperAdmin: false, companyId: 7));

        Assert.Contains("[CompanyId] =", db.Incidents.ToQueryString());
        Assert.Contains("[CompanyId] =", db.Stores.ToQueryString());
        Assert.Contains("[CompanyId] =", db.ResolutionTypes.ToQueryString());
    }

    [Fact]
    public void CallerCompany_IsReadPerContext_NotFrozenAtModelBuild()
    {
        // The model is built once and cached. Each context must still send its own caller's company.
        using var company7 = CreateContext(new FakeCurrentUser(isSuperAdmin: false, companyId: 7));
        using var company9 = CreateContext(new FakeCurrentUser(isSuperAdmin: false, companyId: 9));

        Assert.Contains("= 7;", company7.Incidents.ToQueryString());
        Assert.Contains("= 9;", company9.Incidents.ToQueryString());
    }

    [Fact]
    public void IgnoringTheTenantFilter_RemovesTheCompanyCondition()
    {
        using var db = CreateContext(new FakeCurrentUser(isSuperAdmin: false, companyId: 7));

        var sql = db.Incidents.IgnoreQueryFilters([AppDbContext.TenantFilter]).ToQueryString();

        Assert.DoesNotContain("WHERE", sql);
    }

    [Fact]
    public void Users_AreNotTenantFiltered()
    {
        // Deliberate: login runs before authentication, and SuperAdmin authors must stay visible.
        using var db = CreateContext(new FakeCurrentUser(isSuperAdmin: false, companyId: 7));

        Assert.DoesNotContain("WHERE", db.Users.ToQueryString());
    }
}
