using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIncidentFastTrack.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(500);

        builder.HasOne(s => s.Company)
            .WithMany(c => c.Stores)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
