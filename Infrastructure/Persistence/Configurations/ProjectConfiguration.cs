using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");

        builder.Property(p => p.ProjectName).HasMaxLength(300);
        builder.Property(p => p.ProjectNumber).HasMaxLength(50);
        builder.Property(p => p.ContractNumber).HasMaxLength(50);
        builder.Property(p => p.ProjectOwner).HasMaxLength(200);
    }
}
