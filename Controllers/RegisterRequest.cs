using Microsoft.AspNetCore.Http;

namespace PMIS.Controllers;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EmailCode { get; set; } = string.Empty;
    public string MobileCode { get; set; } = string.Empty;
    public IFormFile? Photo { get; set; }
}
