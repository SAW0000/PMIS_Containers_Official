namespace PMIS.Application.Interfaces;

public interface ILoginLockoutService
{
    Task<bool> IsLockedOutAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> RegisterFailureAsync(string email, CancellationToken cancellationToken = default);

    Task ResetAsync(string email, CancellationToken cancellationToken = default);
}
