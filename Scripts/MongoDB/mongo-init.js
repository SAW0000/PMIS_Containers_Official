
const db = db.getSiblingDB('pmis');

function ensureCollection(name, options) {
  const exists = db.getCollectionNames().includes(name);
  if (exists) {
    db.runCommand({ collMod: name, ...options });
    print(`Collection '${name}' already existed — validator updated.`);
  } else {
    db.createCollection(name, options);
    print(`Collection '${name}' created.`);
  }
}

function ensureIndex(collection, keys, options) {
  collection.createIndex(keys, options);
}


ensureCollection('auditLog', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['entityType', 'entityId', 'action', 'performedBy', 'timestamp', 'ipAddress'],
      properties: {
        entityType: {
          bsonType: 'string',
          enum: ['Request', 'User', 'License', 'Settings', 'Auth'],
          description: 'Type of entity the action was performed against'
        },
        entityId: {
          bsonType: ['binData', 'string'],
          description: 'GUID — SQL PK of the affected row (Users.Id for Auth)'
        },
        action: {
          bsonType: 'string',
          description: 'Canonical machine action code, e.g. RequestSubmitted'
        },
        requestId: {
          bsonType: ['binData', 'string', 'null'],
          description: 'GUID|null — set whenever the action concerns a request'
        },
        requestNumberSnapshot: {
          bsonType: ['string', 'null'],
          description: 'Display snapshot for the admin log & timeline'
        },
        performedBy: {
          bsonType: 'object',
          required: ['userId', 'fullNameSnapshot', 'role'],
          properties: {
            userId: { bsonType: ['binData', 'string'] },
            fullNameSnapshot: { bsonType: 'string' },
            role: { bsonType: 'string' }
          }
        },
        fromState: {
          bsonType: ['string', 'null'],
          description: 'Canonical SQL state name, only for transitions'
        },
        toState: {
          bsonType: ['string', 'null']
        },
        note: {
          bsonType: ['string', 'null'],
          description: "Officer's free-text note (return/reject)"
        },
        reasonCategory: {
          bsonType: ['string', 'null'],
          enum: ['documents', 'contract', 'ineligible', 'other', null],
          description: 'Mirrors RequestNotes.ReasonCategory'
        },
        timestamp: {
          bsonType: 'date'
        },
        ipAddress: {
          bsonType: 'string',
          description: 'Required — admin log column'
        }
      }
    }
  },
  validationLevel: 'moderate',
  validationAction: 'error'
});

const auditLog = db.getCollection('auditLog');

ensureIndex(auditLog, { timestamp: -1 }, { name: 'ix_auditLog_timestamp_desc' });

ensureIndex(auditLog, { timestamp: -1, _id: -1 }, { name: 'ix_auditLog_timestamp_id_desc' });

ensureIndex(auditLog, { requestId: 1, timestamp: 1 }, { name: 'ix_auditLog_requestId_timestamp' });

ensureIndex(
  auditLog,
  { entityType: 1, entityId: 1, timestamp: -1 },
  { name: 'ix_auditLog_entityType_entityId_timestamp' }
);

ensureIndex(
  auditLog,
  { 'performedBy.userId': 1, timestamp: -1 },
  { name: 'ix_auditLog_performedByUserId_timestamp' }
);


ensureCollection('notifications', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['userId', 'type', 'title', 'message', 'isRead', 'createdDate'],
      properties: {
        userId: {
          bsonType: ['binData', 'string'],
          description: 'GUID — recipient (SQL Users.Id)'
        },
        requestId: {
          bsonType: ['binData', 'string', 'null'],
          description: 'GUID|null — the request this concerns'
        },
        requestNumberSnapshot: {
          bsonType: ['string', 'null']
        },
        type: {
          bsonType: 'string',
          enum: [
            'RequestSubmitted',
            'ReturnedForCorrection',
            'Rejected',
            'AwaitingPayment',
            'PaymentConfirmed',
            'Activated',
            'RequestCancelled',
            'NewRequestArrived',
            'AccountChanged',
            'OwnAccountChanged',
            'NewAuditLogEntry',
            'OverdueRequestsDigest',
            'PaymentConfirmationNeededDigest'
          ],
          description: 'Also selects the UI icon'
        },
        title: {
          bsonType: 'string',
          description: 'Composed and frozen at creation time'
        },
        message: {
          bsonType: 'string',
          description: 'Composed and frozen at creation time'
        },
        isRead: {
          bsonType: 'bool'
        },
        readDate: {
          bsonType: ['date', 'null'],
          description: 'Set when marked read'
        },
        createdDate: {
          bsonType: 'date'
        },
        suppressedByPreference: {
          bsonType: ['bool', 'null'],
          description: 'True when the recipient had this kind switched off — kept for history, hidden from every UI read path'
        },
        delivery: {
          bsonType: ['object', 'null'],
          properties: {
            email: {
              bsonType: 'object',
              properties: {
                status: { bsonType: 'string', enum: ['Pending', 'Sent', 'Failed'] },
                sentAt: { bsonType: ['date', 'null'] },
                error: { bsonType: ['string', 'null'] }
              }
            },
            sms: {
              bsonType: 'object',
              properties: {
                status: { bsonType: 'string', enum: ['Pending', 'Sent', 'Failed'] },
                sentAt: { bsonType: ['date', 'null'] },
                error: { bsonType: ['string', 'null'] }
              }
            }
          }
        }
      }
    }
  },
  validationLevel: 'moderate',
  validationAction: 'error'
});

const notifications = db.getCollection('notifications');

ensureIndex(notifications, { userId: 1, createdDate: -1 }, { name: 'ix_notifications_userId_createdDate' });

ensureIndex(
  notifications,
  { userId: 1, createdDate: -1, _id: -1, suppressedByPreference: 1 },
  { name: 'ix_notifications_userId_createdDate_id' }
);

ensureIndex(notifications, { userId: 1, isRead: 1 }, { name: 'ix_notifications_userId_isRead' });

ensureIndex(
  notifications,
  { 'delivery.email.status': 1 },
  {
    name: 'ix_notifications_delivery_email_status_pending',
    partialFilterExpression: { 'delivery.email.status': 'Pending' }
  }
);
ensureIndex(
  notifications,
  { 'delivery.sms.status': 1 },
  {
    name: 'ix_notifications_delivery_sms_status_pending',
    partialFilterExpression: { 'delivery.sms.status': 'Pending' }
  }
);


print('MongoDB structure migration for PMIS complete.');
print(`Collections: ${db.getCollectionNames().join(', ')}`);
