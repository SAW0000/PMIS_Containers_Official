using System.Text.RegularExpressions;

namespace PMIS.Domain.ValueObjects;

public static partial class MobileFormat
{
    [GeneratedRegex(@"^05\d{8}$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? mobile) => !string.IsNullOrWhiteSpace(mobile) && Pattern().IsMatch(mobile);
}
