namespace PMIS.Application.DTOs;

public class SendSmsRequest
{
    public List<string> MobileNumbers { get; set; } = [];
    public string sms_msg { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string WsKey { get; set; } = string.Empty;
}
