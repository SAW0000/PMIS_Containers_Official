namespace PMIS.Domain.Entities;

public class TargetSystem : Entity
{
    public string SystemName { get; set; } = string.Empty;

    public bool IsDelete { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
}
