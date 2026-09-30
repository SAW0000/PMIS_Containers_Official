using Microsoft.EntityFrameworkCore;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Application.Services;

public class BankAccountService(
    PmisDbContext context, IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IReferenceDataCache cache)
    : IBankAccountService
{
    private const int MaxFieldLength = 200;
    private const int MaxIbanLength = 34;
    private const int MaxAmountLength = 30;
    private const int MaxPurposeDescriptionLength = 200;

    private const string CacheKey = "bank-accounts";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<BankAccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<IReadOnlyList<BankAccountDto>>(CacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var accounts = await context.BankAccounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.CreatedDate)
            .Select(a => ToDto(a))
            .ToListAsync(cancellationToken);

        await cache.SetAsync(CacheKey, accounts, CacheTtl);
        return accounts;
    }

    public async Task<(BankAccountDto? Created, string? Error)> CreateAsync(
        BankAccountInput input, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var (fields, error) = Normalize(input);
        if (error is not null) return (null, error);

        if (await IsIbanTakenAsync(fields.Iban, null, cancellationToken))
        {
            return (null, "يوجد حساب بنفس رقم الآيبان بالفعل");
        }

        var nextSortOrder = await context.BankAccounts
            .Where(a => a.IsActive)
            .Select(a => (int?)a.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var account = new BankAccount
        {
            Iban = fields.Iban,
            BankName = fields.BankName,
            AccountHolderName = fields.AccountHolderName,
            TransferAmount = fields.TransferAmount,
            PurposeDescription = fields.PurposeDescription,
            SortOrder = nextSortOrder + 1
        };
        await unitOfWork.BankAccounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(account, actor, "BankAccountCreated", cancellationToken);

        return (ToDto(account), null);
    }

    public async Task<string?> UpdateAsync(
        Guid id, BankAccountInput input, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var (fields, error) = Normalize(input);
        if (error is not null) return error;

        var account = await FindActiveAsync(id, cancellationToken);
        if (account is null) return "الحساب غير موجود";

        if (await IsIbanTakenAsync(fields.Iban, id, cancellationToken))
        {
            return "يوجد حساب بنفس رقم الآيبان بالفعل";
        }

        account.Iban = fields.Iban;
        account.BankName = fields.BankName;
        account.AccountHolderName = fields.AccountHolderName;
        account.TransferAmount = fields.TransferAmount;
        account.PurposeDescription = fields.PurposeDescription;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(account, actor, "BankAccountUpdated", cancellationToken);
        return null;
    }

    public async Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default)
    {
        var account = await FindActiveAsync(id, cancellationToken);
        if (account is null) return "الحساب غير موجود";

        unitOfWork.BankAccounts.Remove(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheKey);

        await WriteAuditAsync(account, actor, "BankAccountDeleted", cancellationToken);
        return null;
    }

    private static (BankAccountInput Fields, string? Error) Normalize(BankAccountInput input)
    {
        var fields = new BankAccountInput
        {
            Iban = input.Iban?.Trim() ?? string.Empty,
            BankName = input.BankName?.Trim() ?? string.Empty,
            AccountHolderName = input.AccountHolderName?.Trim() ?? string.Empty,
            TransferAmount = input.TransferAmount?.Trim() ?? string.Empty,
            PurposeDescription = input.PurposeDescription?.Trim() ?? string.Empty
        };

        if (string.IsNullOrEmpty(fields.Iban)) return (fields, "رقم الآيبان مطلوب");
        if (fields.Iban.Length > MaxIbanLength) return (fields, $"رقم الآيبان يجب ألا يتجاوز {MaxIbanLength} حرفًا");
        if (string.IsNullOrEmpty(fields.BankName)) return (fields, "إسم البنك مطلوب");
        if (fields.BankName.Length > MaxFieldLength) return (fields, $"إسم البنك يجب ألا يتجاوز {MaxFieldLength} حرفًا");
        if (string.IsNullOrEmpty(fields.AccountHolderName)) return (fields, "إسم المستخدم مطلوب");
        if (fields.AccountHolderName.Length > MaxFieldLength) return (fields, $"إسم المستخدم يجب ألا يتجاوز {MaxFieldLength} حرفًا");
        if (string.IsNullOrEmpty(fields.TransferAmount)) return (fields, "مبلغ التحويل مطلوب");
        if (fields.TransferAmount.Length > MaxAmountLength) return (fields, $"مبلغ التحويل يجب ألا يتجاوز {MaxAmountLength} حرفًا");
        if (string.IsNullOrEmpty(fields.PurposeDescription)) return (fields, "مخصص الى مطلوب");
        if (fields.PurposeDescription.Length > MaxPurposeDescriptionLength) return (fields, $"مخصص الى يجب ألا يتجاوز {MaxPurposeDescriptionLength} حرفًا");

        return (fields, null);
    }

    private Task<bool> IsIbanTakenAsync(string iban, Guid? excludingId, CancellationToken cancellationToken)
        => context.BankAccounts
            .AnyAsync(a => a.IsActive && a.Iban == iban && (excludingId == null || a.Id != excludingId), cancellationToken);

    private Task<BankAccount?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
        => context.BankAccounts.FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);

    private static BankAccountDto ToDto(BankAccount a) => new()
    {
        Id = a.Id,
        Iban = a.Iban,
        BankName = a.BankName,
        AccountHolderName = a.AccountHolderName,
        TransferAmount = a.TransferAmount,
        PurposeDescription = a.PurposeDescription
    };

    private Task WriteAuditAsync(
        BankAccount account, RequestActor actor, string action, CancellationToken cancellationToken)
        => auditLogRepository.AddAsync(new AuditLogEntry
        {
            EntityType = "Settings",
            EntityId = account.Id,
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
