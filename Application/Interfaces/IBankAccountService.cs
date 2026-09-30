using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(BankAccountDto? Created, string? Error)> CreateAsync(
        BankAccountInput input, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> UpdateAsync(
        Guid id, BankAccountInput input, RequestActor actor, CancellationToken cancellationToken = default);

    Task<string?> DeleteAsync(Guid id, RequestActor actor, CancellationToken cancellationToken = default);
}
