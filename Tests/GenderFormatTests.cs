using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;

namespace PMIS.Tests;

public class GenderFormatTests
{
    [Theory]
    [InlineData("M", Gender.Male)]
    [InlineData("m", Gender.Male)]
    [InlineData("Male", Gender.Male)]
    [InlineData("ذكر", Gender.Male)]
    [InlineData("F", Gender.Female)]
    [InlineData("f", Gender.Female)]
    [InlineData("Female", Gender.Female)]
    [InlineData("أنثى", Gender.Female)]
    [InlineData("انثى", Gender.Female)]
    public void Parse_MapsKnownVariants_ToEnum(string raw, Gender expected)
    {
        Assert.Equal(expected, GenderFormat.Parse(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X")]
    [InlineData("unknown")]
    public void Parse_ReturnsNull_ForBlankOrUnrecognized(string? raw)
    {
        Assert.Null(GenderFormat.Parse(raw));
    }

    [Theory]
    [InlineData("ذكر", Gender.Male)]
    [InlineData("أنثى", Gender.Female)]
    public void TryParseArabicLabel_AcceptsOnlyTheCanonicalLabels(string label, Gender expected)
    {
        Assert.True(GenderFormat.TryParseArabicLabel(label, out var gender));
        Assert.Equal(expected, gender);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("Male")]
    [InlineData("M")]
    [InlineData("انثى")]
    [InlineData("")]
    public void TryParseArabicLabel_RejectsAnythingElse(string label)
    {
        Assert.False(GenderFormat.TryParseArabicLabel(label, out _));
    }

    [Fact]
    public void ToArabicLabel_MapsEnumToItsCanonicalLabel()
    {
        Assert.Equal("ذكر", GenderFormat.ToArabicLabel(Gender.Male));
        Assert.Equal("أنثى", GenderFormat.ToArabicLabel(Gender.Female));
    }

    [Fact]
    public void ToArabicLabel_NullableOverload_ReturnsEmpty_ForNull()
    {
        Assert.Equal(string.Empty, GenderFormat.ToArabicLabel((Gender?)null));
    }
}
