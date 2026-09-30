using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PMIS.Domain.Entities;

public class AuditLogEntry
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid? RequestId { get; set; }
    public string? RequestNumberSnapshot { get; set; }

    public AuditLogActor PerformedBy { get; set; } = null!;

    public string? FromState { get; set; }
    public string? ToState { get; set; }
    public string? Note { get; set; }
    public string? ReasonCategory { get; set; }

    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class AuditLogActor
{
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    public string FullNameSnapshot { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
