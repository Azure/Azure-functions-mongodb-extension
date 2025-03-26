# Azure WebJobs CosmosDB Mongo Extensions

This repo contains binding extensions for the Azure WebJobs SDK intended for working with [Azure CosmosDB for Mongo](https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/introduction)'s various APIs. See the [Azure WebJobs SDK repo](https://github.com/Azure/azure-webjobs-sdk) for more information on WebJobs.

## MongoDB Api

To configure the binding, add the Mongo connection string as an app setting or environment variable. The name of the setting can be changed with the `ConnectionStringSetting` property of the binding attribute. An example of usage is [here](/Azure-functions-mongodb-extension/Sample/Sample.cs).

### Output Binding

In this example, the `item` object is upserted into the `ItemCollection` collection of the `ItemDb` database.

```csharp
public static void OutputBindingRun(
   [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
   [CosmosDBMongo(ItemDb, ItemCollection, ConnectionStringSetting = connStr)] out TestClass newItem,
   ILogger log)
{
    newItem = new TestClass()
    {
        id = Guid.NewGuid().ToString(),
        SomeData = "some random data"
    };
}
```

### Input Binding

You can get the documents from a collection with query

```csharp
public static void InputBindingRun(
    [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
    [CosmosDBMongo(db, col, ConnectionStringSetting = connStr,
    QueryString = query)] List<BsonDocument> docs,
    ILogger log)
{
    foreach (var doc in docs)
    {
        log.LogInformation(doc.ToString());
    }
}
```

### Triggers

The trigger sets up a [change stream pipeline](https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/vcore/change-streams?tabs=javascript%2CInsert) to monitor changes on a certain collection. Here is an example


```csharp
public static void TriggerRun(
   [CosmosDBMongoTrigger(db, col, ConnectionStringSetting = connStr)] ChangeStreamDocument<BsonDocument> doc,
   ILogger log)
{
    log.LogInformation(doc.FullDocument.ToString());
}
```

## Private Preview

Until the extension becomes available in the portal and extension bundles, it can be used by directly installing the extension. Use the example project for [C#](Sample) and modify it for your purpose.