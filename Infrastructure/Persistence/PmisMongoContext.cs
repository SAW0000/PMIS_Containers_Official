using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using PMIS.Domain.Entities;

namespace PMIS.Infrastructure.Persistence;

public class PmisMongoContext
{
    private static readonly object ConventionLock = new();
    private static bool _conventionsRegistered;

    public IMongoCollection<AuditLogEntry> AuditLog { get; }
    public IMongoCollection<Notification> Notifications { get; }

    public PmisMongoContext(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        RegisterConventionsOnce();

        var database = client.GetDatabase(settings.Value.DatabaseName);
        AuditLog = database.GetCollection<AuditLogEntry>("auditLog");
        Notifications = database.GetCollection<Notification>("notifications");
    }

    private static void RegisterConventionsOnce()
    {
        if (_conventionsRegistered) return;
        lock (ConventionLock)
        {
            if (_conventionsRegistered) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("PmisCamelCase", pack, _ => true);

            _conventionsRegistered = true;
        }
    }
}
