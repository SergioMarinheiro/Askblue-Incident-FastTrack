using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Data;

public class AppDbContext : DbContext
{
    // Name of the tenant query filter. Switch it off for one query with
    // .IgnoreQueryFilters([AppDbContext.TenantFilter]).
    public const string TenantFilter = "Tenant";

    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    // The tenant filter must read these through the DbContext instance.
    // EF builds the model (and the filter expressions) once and caches it for all contexts.
    // Members of the context are turned into SQL parameters that EF re-reads on every query,
    // so each request gets its own caller. A value captured in a local variable in
    // OnModelCreating would be frozen: every request would see the first caller's company.
    private bool CurrentUserIsSuperAdmin => _currentUser.IsSuperAdmin;
    private int? CurrentCompanyId => _currentUser.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ResolutionType> ResolutionTypes => Set<ResolutionType>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<IncidentAttachment> IncidentAttachments => Set<IncidentAttachment>();
    public DbSet<IncidentHistory> IncidentHistory => Set<IncidentHistory>();
    public DbSet<IncidentDuplicate> IncidentDuplicates => Set<IncidentDuplicate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in Data/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilters(modelBuilder);
    }

    // Tenant isolation for the tables where a leak hurts most. Every query on them gets
    // "WHERE CompanyId = <caller's company>" added, unless the caller is a SuperAdmin.
    // It fails closed: with nobody logged in, CompanyId is null and nothing matches.
    //
    // Deliberately not filtered:
    // - User: login runs before anyone is authenticated, and a filter would hide
    //   SuperAdmin authors (CompanyId null) on comments and history. UserService filters explicitly.
    // - Child entities (Location, comments, history, attachments, duplicates): services load
    //   the parent store or incident first, which goes through this filter.
    //   EF logs a warning that they have a required link to a filtered entity. That is intended:
    //   a child of another company's store or incident must stay hidden too.
    //
    // A filter only affects reads. On create, CompanyId must still be set from ICurrentUser.
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>().HasQueryFilter(TenantFilter,
            e => CurrentUserIsSuperAdmin || e.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<Store>().HasQueryFilter(TenantFilter,
            e => CurrentUserIsSuperAdmin || e.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ResolutionType>().HasQueryFilter(TenantFilter,
            e => CurrentUserIsSuperAdmin || e.CompanyId == CurrentCompanyId);
    }

    public override int SaveChanges()
    {
        SetAuditDates();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditDates();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditDates()
    {
        var now = DateTime.UtcNow;

        // ToList: take a snapshot, because the loop changes entry.State (soft delete)
        foreach (var entry in ChangeTracker.Entries<BaseDomainModel>().ToList())
        {
            if(entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }

            if(entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.ModifiedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }
    }
}

