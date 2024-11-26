using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Sample
{
    public static class Sample
    {
        [FunctionName("OutputBindingSample")]
        public static async Task OutputBindingRun(
           [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
           [CosmosDBMongo("%vCoreDatabaseBinding%", "%vCoreCollectionBinding%", ConnectionStringSetting = "vCoreConnectionStringBinding")] IAsyncCollector<TestClass> CosmosDBMongoCollector,
           ILogger log)
        {
           log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

           TestClass item = new TestClass()
           {
               id = Guid.NewGuid().ToString(),
               SomeData = "some random data"
           };

           await CosmosDBMongoCollector.AddAsync(item);
        }


        [FunctionName("TriggerSample")]
        public static void TriggerRun(
           [CosmosDBMongoTrigger("vCoreDatabaseTrigger", "vCoreCollectionTrigger", ConnectionStringSetting = "vCoreConnectionStringTrigger")] ChangeStreamDocument<BsonDocument> doc,
           ILogger log)
        {
           log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

           log.LogInformation(doc.FullDocument.ToString());
        }

        [FunctionName("InputBindingSample")]
        public static async Task InputBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%vCoreDatabaseTrigger%", "%vCoreCollectionTrigger%", ConnectionStringSetting = "vCoreConnectionStringTrigger",
            QueryString = "%queryString%")] List<BsonDocument> docs,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            foreach (var doc in docs)
            {
                log.LogInformation(doc.ToString());
            }
        }
    }

    public class TestClass
    {
        public string id { get; set; }
        public string SomeData { get; set; }
    }
}
