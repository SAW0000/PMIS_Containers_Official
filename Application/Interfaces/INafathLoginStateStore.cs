using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface INafathLoginStateStore
{
    Task SetAsync(string token, UserRole role, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<UserRole?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default);
}
