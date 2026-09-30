using System.Text.Json;

namespace PMIS.Domain.ValueObjects;

public static class NafathArabicNationalityFormat
{
    public static string ForStorage(JsonElement raw) => raw.ValueKind switch
    {
        JsonValueKind.String => raw.GetString() ?? string.Empty,
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => raw.ToString(),
        _ => string.Empty
    };
}
