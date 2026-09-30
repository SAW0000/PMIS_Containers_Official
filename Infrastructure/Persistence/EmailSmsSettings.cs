namespace PMIS.Infrastructure.Persistence;

public class EmailSmsSettings
{
    public string LmsBaseUrl { get; set; } = string.Empty;

    public string SendMailPath { get; set; } = string.Empty;

    public string MailWsKey { get; set; } = string.Empty;

    public string MailUsername { get; set; } = string.Empty;

    public string MailPassword { get; set; } = string.Empty;

    public string MailFrom { get; set; } = string.Empty;

    public string SendSmsPath { get; set; } = string.Empty;

    public string SmsWsKey { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;
}
