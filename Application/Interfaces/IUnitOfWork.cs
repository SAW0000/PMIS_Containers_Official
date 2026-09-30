using PMIS.Domain.Entities;

namespace PMIS.Application.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<Company> Companies { get; }
    IRepository<Project> Projects { get; }
    IRepository<TargetSystem> TargetSystems { get; }
    IRepository<Request> Requests { get; }
    IRepository<Attachment> Attachments { get; }
    IRepository<RequestNote> RequestNotes { get; }
    IRepository<RejectionReasonOption> RejectionReasonOptions { get; }
    IRepository<BankAccount> BankAccounts { get; }
    IRepository<RequestHistory> RequestHistories { get; }
    IRepository<License> Licenses { get; }
    IRepository<SystemSettings> SystemSettings { get; }
    IRepository<NotificationPreference> NotificationPreferences { get; }
    IRepository<UserRoleAssignment> UserRoleAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
