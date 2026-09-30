using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface INotificationPreferenceService
{
    Task<IReadOnlyList<NotificationPreferenceDto>> GetForUserAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default);

    Task<string?> UpdateAsync(
        Guid userId, UserRole role, IReadOnlyList<NotificationPreferenceDto> preferences,
        CancellationToken cancellationToken = default);

    Task<bool> IsEnabledAsync(Guid userId, string preferenceKey, CancellationToken cancellationToken = default);
}
