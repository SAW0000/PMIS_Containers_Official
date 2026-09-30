using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PmisDbContext _context;

    public UnitOfWork(PmisDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Companies = new Repository<Company>(context);
        Projects = new Repository<Project>(context);
        TargetSystems = new Repository<TargetSystem>(context);
        Requests = new Repository<Request>(context);
        Attachments = new Repository<Attachment>(context);
        RequestNotes = new Repository<RequestNote>(context);
        RejectionReasonOptions = new Repository<RejectionReasonOption>(context);
        BankAccounts = new Repository<BankAccount>(context);
        RequestHistories = new Repository<RequestHistory>(context);
        Licenses = new Repository<License>(context);
        SystemSettings = new Repository<SystemSettings>(context);
        NotificationPreferences = new Repository<NotificationPreference>(context);
        UserRoleAssignments = new Repository<UserRoleAssignment>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Company> Companies { get; }
    public IRepository<Project> Projects { get; }
    public IRepository<TargetSystem> TargetSystems { get; }
    public IRepository<Request> Requests { get; }
    public IRepository<Attachment> Attachments { get; }
    public IRepository<RequestNote> RequestNotes { get; }
    public IRepository<RejectionReasonOption> RejectionReasonOptions { get; }
    public IRepository<BankAccount> BankAccounts { get; }
    public IRepository<RequestHistory> RequestHistories { get; }
    public IRepository<License> Licenses { get; }
    public IRepository<SystemSettings> SystemSettings { get; }
    public IRepository<NotificationPreference> NotificationPreferences { get; }
    public IRepository<UserRoleAssignment> UserRoleAssignments { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
