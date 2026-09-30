namespace PMIS.Application.Interfaces;

public interface IReferenceDataCache
{
    Task<T?> GetAsync<T>(string key);

    Task SetAsync<T>(string key, T value, TimeSpan ttl);

    Task InvalidateAsync(string key);
}
