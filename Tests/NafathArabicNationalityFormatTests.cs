using System.Text.Json;
using PMIS.Domain.ValueObjects;

namespace PMIS.Tests;

public class NafathArabicNationalityFormatTests
{
    [Fact]
    public void ForStorage_ReturnsTheString_WhenValueIsAJsonString()
    {
        var element = JsonSerializer.SerializeToElement("سعودي");

        Assert.Equal("سعودي", NafathArabicNationalityFormat.ForStorage(element));
    }

    [Fact]
    public void ForStorage_ReturnsEmpty_WhenValueIsJsonNull()
    {
        var element = JsonSerializer.SerializeToElement<string?>(null);

        Assert.Equal(string.Empty, NafathArabicNationalityFormat.ForStorage(element));
    }

    [Fact]
    public void ForStorage_ReturnsEmpty_WhenValueIsANestedObject()
    {
        var element = JsonSerializer.SerializeToElement(new { code = 1, label = "سعودي" });

        Assert.Equal(string.Empty, NafathArabicNationalityFormat.ForStorage(element));
    }

    [Fact]
    public void ForStorage_ReturnsEmpty_ForTheDefaultUninitializedElement()
    {
        JsonElement element = default;

        Assert.Equal(string.Empty, NafathArabicNationalityFormat.ForStorage(element));
    }

    [Fact]
    public void ForStorage_ConvertsANumber_RatherThanThrowing()
    {
        var element = JsonSerializer.SerializeToElement(966);

        Assert.Equal("966", NafathArabicNationalityFormat.ForStorage(element));
    }
}
