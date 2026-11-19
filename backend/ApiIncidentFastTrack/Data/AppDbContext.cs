using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

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
}
