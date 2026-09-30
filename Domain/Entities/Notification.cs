using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PMIS.Domain.Entities;

public class Notification
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid? RequestId { get; set; }
    public string? RequestNumberSnapshot { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime CreatedDate { get; set; }

    public bool SuppressedByPreference { get; set; }

    public NotificationDelivery? Delivery { get; set; }
}

public class NotificationDelivery
{
    public NotificationDeliveryChannel Email { get; set; } = new();
    public NotificationDeliveryChannel Sms { get; set; } = new();
}

public class NotificationDeliveryChannel
{
    public string Status { get; set; } = "Pending";
    public DateTime? SentAt { get; set; }
    public string? Error { get; set; }
}
