namespace PMIS.Domain.Entities;

public class Company : Entity
{
    public string? CompanyName { get; set; }
    public string? CommercialNo { get; set; }
    public string? CompanyActivity { get; set; }
    public string? VatNo { get; set; }
    public string? NationalAddress { get; set; }
    public string? Iban { get; set; }
    public string? LogoFileRef { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
}
