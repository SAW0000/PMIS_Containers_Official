namespace PMIS.Controllers;

public class SendRegistrationOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
}
