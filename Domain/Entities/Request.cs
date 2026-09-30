using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class Request : Entity
{
    public string RequestNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? ApplicantFirstName { get; set; }
    public string? ApplicantLastName { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? ApplicantMobile { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? SystemId { get; set; }
    public TargetSystem? System { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
    public DateTime? ReviewDate { get; set; }

    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }

    public string? Decision { get; set; }
    public string? Notes { get; set; }

    public UserTypeDisplay? UserTypeDisplay { get; set; }

    public DateTime? PaymentConfirmedDate { get; set; }

    public byte[]? RowVersion { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<RequestNote> RequestNotes { get; set; } = [];
    public ICollection<RequestHistory> RequestHistories { get; set; } = [];
    public License? License { get; set; }
}
