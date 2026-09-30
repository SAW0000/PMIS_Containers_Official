
const db = db.getSiblingDB('pmis');

const result = db.auditLog.deleteMany({});
print(`Deleted ${result.deletedCount} auditLog documents (entire collection cleared).`);
print(`Remaining auditLog: ${db.auditLog.countDocuments({})}`);
