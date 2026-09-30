namespace PMIS.Application.DTOs;

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string TransferAmount { get; set; } = string.Empty;
    public string PurposeDescription { get; set; } = string.Empty;
}

public class BankAccountInput
{
    public string Iban { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string TransferAmount { get; set; } = string.Empty;
    public string PurposeDescription { get; set; } = string.Empty;
}
