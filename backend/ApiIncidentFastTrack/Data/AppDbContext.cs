using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

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

