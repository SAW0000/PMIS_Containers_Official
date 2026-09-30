using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class License : Entity
{
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public string? LicenseKey { get; set; }
    public int NoOfLicenses { get; set; } = 1;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ActivationDate { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;

    public byte[]? RowVersion { get; set; }
}
