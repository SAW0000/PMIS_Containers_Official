using System.Text.RegularExpressions;

namespace PMIS.Domain.ValueObjects;

public static partial class TextInput
{
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]")]
    private static partial Regex ControlCharacterPattern();

    public static bool HasControlCharacters(string value) => ControlCharacterPattern().IsMatch(value);
}
