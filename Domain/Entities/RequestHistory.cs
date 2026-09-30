namespace PMIS.Domain.Entities;

public class RequestHistory : Entity
{
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public string EventLabel { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}
