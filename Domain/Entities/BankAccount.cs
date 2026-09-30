namespace PMIS.Domain.Entities;

public class BankAccount : Entity
{
    public string Iban { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string AccountHolderName { get; set; } = string.Empty;

    public string TransferAmount { get; set; } = string.Empty;

    public string PurposeDescription { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
