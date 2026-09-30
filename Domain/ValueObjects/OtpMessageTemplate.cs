namespace PMIS.Domain.ValueObjects;

public static class OtpMessageTemplate
{
    public const string EmailToken = "@MAIL";

    public const string SmsToken = "@SMS";

    public static bool Contains(string template, string token) =>
        template.Contains(token, StringComparison.OrdinalIgnoreCase);

    public static string Substitute(string template, string token, string code) =>
        System.Text.RegularExpressions.Regex.Replace(
            template,
            System.Text.RegularExpressions.Regex.Escape(token),
            code.Replace("$", "$$"), 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
