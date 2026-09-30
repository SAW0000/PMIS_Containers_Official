using System.Text.RegularExpressions;

namespace PMIS.Domain.ValueObjects;

public static partial class EmailFormat
{
    [GeneratedRegex(@"^[^@\s<>""'&`]+@[^@\s<>""'&`]+\.[^@\s<>""'&`]+$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? email) => !string.IsNullOrWhiteSpace(email) && Pattern().IsMatch(email);
}
