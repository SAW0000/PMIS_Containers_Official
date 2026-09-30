namespace PMIS.Application.DTOs;

public class RequestDraftDetailsDto
{
    public Guid RequestId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CommercialNo { get; set; } = string.Empty;
    public string? CompanyActivity { get; set; }
    public Guid? SystemId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
}
