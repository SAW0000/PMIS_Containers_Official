namespace PMIS.Domain.Entities;

public class Project : Entity
{
    public string? ProjectName { get; set; }
    public string? ProjectNumber { get; set; }
    public string? ContractNumber { get; set; }
    public string? ProjectOwner { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
}
