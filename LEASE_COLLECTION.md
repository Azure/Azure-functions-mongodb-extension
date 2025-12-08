# Lease Collection Feature for MongoDB Trigger

## Overview

The lease collection feature enables better scaling and reliable event processing for MongoDB triggers by using a separate collection to store and consume change stream events.

## Architecture

The lease collection implements a producer-consumer pattern:

- **Producer**: A background task watches the change stream and writes events to the lease collection
- **Consumer**: Multiple worker threads read events from the lease collection in timestamp order and execute the trigger function
- **Metrics**: The scale controller queries the lease collection directly for accurate pending event counts

## Configuration

To enable the lease collection feature, add the following properties to your `CosmosDBMongoTrigger` attribute:

```csharp
[CosmosDBMongoTrigger(
    databaseName: "myDatabase",
    collectionName: "myCollection",
    ConnectionStringSetting = "MongoConnection",
    LeaseDatabaseName = "leases",
    LeaseCollectionName = "myTriggerLeases",
    LeaseConnectionStringSetting = "LeaseMongoConnection")]  // Optional, defaults to MongoConnection
public void Run([CosmosDBMongoTrigger] ChangeStreamDocument<BsonDocument> document, ILogger log)
{
    log.LogInformation($"Document changed: {document.FullDocument}");
}
```

### Properties

- **LeaseDatabaseName**: Database name for storing lease documents (required for lease collection mode)
- **LeaseCollectionName**: Collection name for storing lease documents (required for lease collection mode)
- **LeaseConnectionStringSetting**: Connection string setting for the lease cluster (optional, defaults to the monitored cluster connection)

## Benefits

1. **Improved Scaling**: The scale controller can accurately count pending events in the lease collection
2. **Event Ordering**: Events are processed in timestamp order
3. **Separate Scaling**: The lease collection can be on a different cluster with different performance characteristics
4. **Durability**: Events are persisted before processing, enabling recovery after failures
5. **Visibility**: Administrators can query the lease collection to monitor pending events

## Backward Compatibility

If lease collection properties are not specified, the trigger uses the existing in-memory behavior with no changes required.

## Implementation Details

### Lease Document Schema

Each lease document contains:
- `timestamp`: When the event was captured (used for ordering)
- `monitorLevel`: The level of monitoring (Collection/Database/Cluster)
- `sourceCluster`: The monitored cluster identifier
- `sourceDatabase`: The source database name
- `sourceCollection`: The source collection name
- `functionId`: The function identifier
- `resumeToken`: The change stream resume token for recovery
- `changeEvent`: The serialized change stream document
- `createdAt`: When the lease document was created

### Indexes

The lease collection is initialized with the following indexes:
1. `{ timestamp: 1, functionId: 1 }` - For ordered consumption
2. `{ functionId: 1, sourceDatabase: 1, sourceCollection: 1, timestamp: 1 }` - For filtering by source

### Error Handling

- **Producer errors**: The producer retries failed insertions with exponential backoff (up to 3 attempts)
- **Consumer errors**: If parsing fails, the event is logged and skipped to avoid infinite loops with corrupted data
- **Function execution errors**: Handled by the executor's built-in retry mechanism

## Example Scenarios

### Same Cluster for Monitoring and Leases

```csharp
[CosmosDBMongoTrigger(
    databaseName: "myDatabase",
    collectionName: "myCollection",
    ConnectionStringSetting = "MongoConnection",
    LeaseDatabaseName = "leases",
    LeaseCollectionName = "myTriggerLeases")]
```

### Different Cluster for Leases

```csharp
[CosmosDBMongoTrigger(
    databaseName: "myDatabase",
    collectionName: "myCollection",
    ConnectionStringSetting = "MongoConnection",
    LeaseDatabaseName = "leases",
    LeaseCollectionName = "myTriggerLeases",
    LeaseConnectionStringSetting = "LeaseMongoConnection")]
```

### No Lease Collection (Backward Compatible)

```csharp
[CosmosDBMongoTrigger(
    databaseName: "myDatabase",
    collectionName: "myCollection",
    ConnectionStringSetting = "MongoConnection")]
```

## Monitoring

You can monitor the lease collection using standard MongoDB tools:

```javascript
// Count pending events for a specific function
db.myTriggerLeases.countDocuments({
  functionId: "MyTriggerFunction",
  sourceDatabase: "myDatabase",
  sourceCollection: "myCollection"
})

// View oldest pending events
db.myTriggerLeases.find({
  functionId: "MyTriggerFunction"
}).sort({ timestamp: 1 }).limit(10)
```
