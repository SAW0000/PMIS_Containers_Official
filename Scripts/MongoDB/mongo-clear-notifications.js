
const db = db.getSiblingDB('pmis');

const result = db.notifications.deleteMany({});
print(`Deleted ${result.deletedCount} notifications documents (entire collection cleared).`);
print(`Remaining notifications: ${db.notifications.countDocuments({})}`);
