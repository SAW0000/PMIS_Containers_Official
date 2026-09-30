using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<string?> UpdateAsync(
        SystemSettingsDto settings, RequestActor actor, CancellationToken cancellationToken = default);
}
