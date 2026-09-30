using System.Text.RegularExpressions;

namespace PMIS.Domain.ValueObjects;

public static partial class NationalIdFormat
{
    [GeneratedRegex(@"^[12]\d{9}$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? nationalId) => !string.IsNullOrWhiteSpace(nationalId) && Pattern().IsMatch(nationalId);
}
