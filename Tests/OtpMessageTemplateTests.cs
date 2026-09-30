using PMIS.Domain.ValueObjects;

namespace PMIS.Tests;

public class OtpMessageTemplateTests
{
    [Theory]
    [InlineData("يرجى إدخال رمز التحقق: @MAIL", "@MAIL", true)]
    [InlineData("يرجى إدخال رمز التحقق: @mail", "@MAIL", true)] 
    [InlineData("يرجى إدخال رمز التحقق: @Mail", "@MAIL", true)] 
    [InlineData("لا يوجد رمز هنا", "@MAIL", false)]
    [InlineData("", "@MAIL", false)]
    public void Contains_IsCaseInsensitive(string template, string token, bool expected)
    {
        Assert.Equal(expected, OtpMessageTemplate.Contains(template, token));
    }

    [Fact]
    public void Substitute_ReplacesToken_WithCode()
    {
        var result = OtpMessageTemplate.Substitute("يرجى إدخال رمز التحقق: @MAIL", OtpMessageTemplate.EmailToken, "482913");

        Assert.Equal("يرجى إدخال رمز التحقق: 482913", result);
    }

    [Fact]
    public void Substitute_IsCaseInsensitive()
    {
        var result = OtpMessageTemplate.Substitute("رمزك: @mail", OtpMessageTemplate.EmailToken, "482913");

        Assert.Equal("رمزك: 482913", result);
    }

    [Fact]
    public void Substitute_ReplacesEveryOccurrence()
    {
        var result = OtpMessageTemplate.Substitute("@SMS @SMS", OtpMessageTemplate.SmsToken, "111222");

        Assert.Equal("111222 111222", result);
    }

    [Fact]
    public void Substitute_TreatsCodeAsLiteral_NotRegexBackreference()
    {
        var result = OtpMessageTemplate.Substitute("رمزك: @SMS", OtpMessageTemplate.SmsToken, "$1");

        Assert.Equal("رمزك: $1", result);
    }
}
