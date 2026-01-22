// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public static class Sample
    {
        [FunctionName("EntraIdAuthSample")]
        public static void EntraIdAuthRun(
                [TimerTrigger("*/30 * * * * *")] TimerInfo myTimer,
                [CosmosDBMongo(
                    ConnectionStringSetting = "EntraIdConnection",
                    TenantId = "%TenantId%")] IMongoClient client,
                ILogger log)
        {
            log.LogInformation($"Entra ID Auth Sample executed at: {DateTime.Now}");

            try
            {
                var databases = client.ListDatabaseNames().ToList();
                log.LogInformation($"Connected via Entra ID. Found {databases.Count} database(s):");

                foreach (var dbName in databases)
                {
                log.LogInformation($"  Database: {dbName}");
                var db = client.GetDatabase(dbName);
                var collections = db.ListCollectionNames().ToList();

                foreach (var collName in collections)
                {
                    log.LogInformation($"    - Collection: {collName}");
                }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to connect with Entra ID authentication");
            }
        }

        [FunctionName("ClientBindingSample")]
        public static void ClientBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo(ConnectionStringSetting = "MongoDBConnection")] IMongoClient client,
            ILogger log)
        {
            var documents = client.GetDatabase("TestDatabase")
                .GetCollection<BsonDocument>("TestCollection")
                .Find(new BsonDocument())
                .ToList();

            foreach (BsonDocument d in documents)
            {
                log.LogInformation(d.ToString());
            }
        }

        [FunctionName("OutputBindingSample")]
        public static async Task OutputBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%DatabaseName%", "%CollectionName%",
                    ConnectionStringSetting = "MongoDBConnection")] IAsyncCollector<TestClass> collector,
            ILogger log)
        {
            log.LogInformation($"Output binding sample executed at: {DateTime.Now}");

            TestClass item = new TestClass()
            {
                id = Guid.NewGuid().ToString(),
                SomeData = "some random data"
            };

            await collector.AddAsync(item);
        }

        [FunctionName("InputBindingSample")]
        public static void InputBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%DatabaseName%", "%CollectionName%",
                    ConnectionStringSetting = "EntraIdConnection", TenantId = "%TenantId%",
                    QueryString = "%QueryString%")] List<BsonDocument> docs,
            ILogger log)
        {
            log.LogInformation($"Input binding sample executed at: {DateTime.Now}");

            foreach (var doc in docs)
            {
                log.LogInformation(doc.ToString());
            }
        }

        [FunctionName("TriggerSample")]
        public static void TriggerRun(
            [CosmosDBMongoTrigger("%DatabaseName%", "%CollectionName%",
                ConnectionStringSetting = "EntraIdConnection",
                TenantId = "%TenantId%",
                LeaseConnectionStringSetting = "MongoDBConnection")]
            ChangeStreamDocument<BsonDocument> doc,
            ILogger log)
        {
            log.LogInformation($"Change detected at: {DateTime.Now}");
            log.LogInformation(doc.FullDocument.ToString());
        }
    }

    public class TestClass
    {
        public string id { get; set; }
        public string SomeData { get; set; }
    }
}
