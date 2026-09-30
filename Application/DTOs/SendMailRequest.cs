namespace PMIS.Application.DTOs;

public class SendMailRequest
{
    public List<string> MailTo { get; set; } = [];
    public List<string> MailCC { get; set; } = [];
    public string MailSubject { get; set; } = string.Empty;
    public string MailBody { get; set; } = string.Empty;
    public string UName { get; set; } = string.Empty;
    public string UPass { get; set; } = string.Empty;
    public string MailFrom { get; set; } = string.Empty;

    public string DisplaySybject { get; set; } = string.Empty;

    public string WSKey { get; set; } = string.Empty;
}
