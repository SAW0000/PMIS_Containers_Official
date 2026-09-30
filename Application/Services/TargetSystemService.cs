using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class TargetSystemService(
    PmisDbContext context, IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IReferenceDataCache cache)
    : ITargetSystemService
{
    private const int MaxSystemNameLength = 200;

    private const string ActiveSystemsCacheKey = "target-systems-active";

    public async Task<IReadOnlyList<TargetSystemAdminDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        => await context.TargetSystems
            .Where(s => !s.IsDelete)
            .OrderBy(s => s.CreatedDate)
            .Select(s => ToDto(s))
            .ToListAsync(cancellationToken);

    public async Task<(TargetSystemAdminDto? Created, string? Error)> CreateAsync(
        TargetSystemInput input, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var (name, error) = Normalize(input);
        if (error is not null) return (null, error);

        if (await IsNameTakenAsync(name, null, cancellationToken))
        {
            return (null, "يوجد نظام بنفس هذا الإسم بالفعل");
        }

        var system = new TargetSystem { SystemName = name };
        await unitOfWork.TargetSystems.AddAsync(system, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(ActiveSystemsCacheKey);

        await WriteAuditAsync(system, actor, "TargetSystemCreated", cancellationToken);

        return (ToDto(system), null);
    }

    public async Task<string?> SetActiveAsync(
        Guid id, bool isActive, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var system = await FindAsync(id, cancellationToken);
        if (system is null) return "النظام غير موجود";

        system.IsActive = isActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(ActiveSystemsCacheKey);

        await WriteAuditAsync(system, actor, isActive ? "TargetSystemEnabled" : "TargetSystemDisabled", cancellationToken);
        return null;
    }

    public async Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var system = await FindAsync(id, cancellationToken);
        if (system is null) return "النظام غير موجود";

        system.IsDelete = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(ActiveSystemsCacheKey);

        await WriteAuditAsync(system, actor, "TargetSystemDeleted", cancellationToken);
        return null;
    }

    private static (string Name, string? Error) Normalize(TargetSystemInput input)
    {
        var name = input.SystemName?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name)) return (name, "إسم النظام مطلوب");
        if (name.Length > MaxSystemNameLength) return (name, $"إسم النظام يجب ألا يتجاوز {MaxSystemNameLength} حرفًا");

        return (name, null);
    }

    private Task<bool> IsNameTakenAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
        => context.TargetSystems
            .AnyAsync(s => !s.IsDelete && s.SystemName == name && (excludingId == null || s.Id != excludingId), cancellationToken);

    private Task<TargetSystem?> FindAsync(Guid id, CancellationToken cancellationToken)
        => context.TargetSystems.FirstOrDefaultAsync(s => s.Id == id && !s.IsDelete, cancellationToken);

    private static TargetSystemAdminDto ToDto(TargetSystem s) => new()
    {
        Id = s.Id,
        SystemName = s.SystemName,
        IsActive = s.IsActive
    };

    private Task WriteAuditAsync(
        TargetSystem system, RequestActor actor, string action, CancellationToken cancellationToken)
        => auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Settings",
            EntityId = system.Id,
            Action = action,
            PerformedBy = new AuditLogActor
            {
                UserId = actor.UserId,
                FullNameSnapshot = actor.FullName,
                Role = actor.Role.ToString()
            },
            Timestamp = DateTime.UtcNow,
            IpAddress = actor.IpAddress
        }, cancellationToken);
}
