using PMIS.Domain.Enums;

namespace PMIS.Domain.ValueObjects;

public static class GenderFormat
{
    private const string MaleLabel = "ذكر";
    private const string FemaleLabel = "أنثى";

    public static Gender? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" or MaleLabel => Gender.Male,
            "F" or "FEMALE" or FemaleLabel or "انثى" => Gender.Female,
            _ => null
        };
    }

    public static bool TryParseArabicLabel(string label, out Gender gender)
    {
        switch (label)
        {
            case MaleLabel:
                gender = Gender.Male;
                return true;
            case FemaleLabel:
                gender = Gender.Female;
                return true;
            default:
                gender = default;
                return false;
        }
    }

    public static string ToArabicLabel(Gender gender) => gender switch
    {
        Gender.Male => MaleLabel,
        Gender.Female => FemaleLabel,
        _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unmapped Gender value")
    };

    public static string ToArabicLabel(Gender? gender) => gender.HasValue ? ToArabicLabel(gender.Value) : string.Empty;
}
