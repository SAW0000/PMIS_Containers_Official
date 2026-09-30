using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class RejectionReasonService(
    PmisDbContext context, IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IReferenceDataCache cache)
    : IRejectionReasonService
{
    private const int MaxLabelLength = 80;
    private const int MaxNoteLength = 2000;

    private const string CacheKey = "rejection-reasons";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<RejectionReasonOptionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<RejectionReasonOptionDto>>(CacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var reasons = await context.RejectionReasonOptions
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.CreatedDate)
            .Select(o => new RejectionReasonOptionDto
            {
                Id = o.Id,
                Label = o.Label,
                DefaultNote = o.DefaultNote,
                Hint = o.Hint
            })
            .ToListAsync(cancellationToken);

        await cache.SetAsync(CacheKey, reasons, CacheTtl);
        return reasons;
    }

    public async Task<(RejectionReasonOptionDto? Created, string? Error)> CreateAsync(
        RejectionReasonOptionInput input, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var (label, note, error) = Normalize(input);
        if (error is not null) return (null, error);

        if (await IsLabelTakenAsync(label, null, cancellationToken))
        {
            return (null, "يوجد سبب بنفس هذا العنوان بالفعل");
        }

        var option = new RejectionReasonOption { Label = label, DefaultNote = note };
        await unitOfWork.RejectionReasonOptions.AddAsync(option, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(option, actor, "RejectionReasonCreated", cancellationToken);

        return (new RejectionReasonOptionDto
        {
            Id = option.Id,
            Label = option.Label,
            DefaultNote = option.DefaultNote,
            Hint = option.Hint
        }, null);
    }

    public async Task<string?> UpdateAsync(
        Guid id, RejectionReasonOptionInput input, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var (label, note, error) = Normalize(input);
        if (error is not null) return error;

        var option = await FindActiveAsync(id, cancellationToken);
        if (option is null) return "السبب غير موجود";

        if (await IsLabelTakenAsync(label, id, cancellationToken))
        {
            return "يوجد سبب بنفس هذا العنوان بالفعل";
        }

        option.Label = label;
        option.DefaultNote = note;
        option.Hint = null;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(option, actor, "RejectionReasonUpdated", cancellationToken);
        return null;
    }

    public async Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var option = await FindActiveAsync(id, cancellationToken);
        if (option is null) return "السبب غير موجود";

        var remaining = await context.RejectionReasonOptions.CountAsync(o => o.IsActive, cancellationToken);
        if (remaining <= 1)
        {
            return "لا يمكن حذف آخر سبب — يجب أن يبقى سبب واحد على الأقل";
        }

        var citingNotes = await context.RequestNotes
            .Where(n => n.ReasonId == id)
            .ToListAsync(cancellationToken);

        foreach (var note in citingNotes)
        {
            unitOfWork.RequestNotes.Remove(note);
        }

        unitOfWork.RejectionReasonOptions.Remove(option);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(option, actor, "RejectionReasonDeleted", cancellationToken);
        return null;
    }

    private static (string Label, string Note, string? Error) Normalize(RejectionReasonOptionInput input)
    {
        var label = input.Label?.Trim() ?? string.Empty;
        var note = input.DefaultNote?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(label)) return (label, note, "عنوان السبب مطلوب");
        if (label.Length > MaxLabelLength) return (label, note, $"عنوان السبب يجب ألا يتجاوز {MaxLabelLength} حرفًا");
        if (note.Length > MaxNoteLength) return (label, note, $"الوصف الافتراضي يجب ألا يتجاوز {MaxNoteLength} حرفًا");

        return (label, note, null);
    }

    private Task<bool> IsLabelTakenAsync(string label, Guid? excludingId, CancellationToken cancellationToken)
        => context.RejectionReasonOptions
            .AnyAsync(o => o.IsActive && o.Label == label && (excludingId == null || o.Id != excludingId), cancellationToken);

    private Task<RejectionReasonOption?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
        => context.RejectionReasonOptions.FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

    private Task WriteAuditAsync(
        RejectionReasonOption option, RequestActor actor, string action, CancellationToken cancellationToken)
        => auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Settings",
            EntityId = option.Id,
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
