namespace PMIS.Application.Interfaces;

public interface INafathPendingProfileStore
{
    Task SetAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<Guid?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default);
}
