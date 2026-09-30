using Microsoft.EntityFrameworkCore;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence;

public class PmisDbContext(DbContextOptions<PmisDbContext> options) : DbContext(options)
{
    private const string RowIdPropertyName = "RowId";

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TargetSystem> TargetSystems => Set<TargetSystem>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RequestNote> RequestNotes => Set<RequestNote>();
    public DbSet<RejectionReasonOption> RejectionReasonOptions => Set<RejectionReasonOption>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<RequestHistory> RequestHistories => Set<RequestHistory>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PmisDbContext).Assembly);

        ConfigureRowIdClustering(modelBuilder);
    }

    private static void ConfigureRowIdClustering(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entityType.IsOwned() || !typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property<long>(RowIdPropertyName).ValueGeneratedOnAdd();
            builder.HasIndex(RowIdPropertyName).IsUnique().IsClustered();

            entityType.FindPrimaryKey()?.SetIsClustered(false);
        }
    }
}
