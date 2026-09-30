using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence.Configurations;

public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.Property(ra => ra.Id).HasDefaultValueSql("NEWID()");

        builder.Ignore(ra => ra.IsActive);

        builder.Property(ra => ra.Role).IsRequired().HasConversion<string>().HasMaxLength(30);

        builder.HasOne(ra => ra.User)
            .WithMany(u => u.RoleAssignments)
            .HasForeignKey(ra => ra.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ra => new { ra.UserId, ra.Role }).IsUnique();
    }
}
