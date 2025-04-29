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
        [FunctionName("ClientBindingSample")]
        public static void ClientBindingRun(
         [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
         [CosmosDBMongo] MongoClient client,
         ILogger log)
        {
          var documents = client.GetDatabase("%vCoreDatabaseBinding%").GetCollection<BsonDocument>("%vCoreCollectionBinding%").Find(new BsonDocument()).ToList();

          foreach (BsonDocument d in documents)
          {
              log.LogInformation(d.ToString());
          }
        }

        [FunctionName("OutputBindingSample")]
        public static async Task OutputBindingRun(
         [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
         [CosmosDBMongo("%vCoreDatabaseBinding%", "%vCoreCollectionBinding%")] IAsyncCollector<TestClass> CosmosDBMongoCollector,
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
          [CosmosDBMongoTrigger("TestDatabase", "TestCollection")] ChangeStreamDocument<BsonDocument> doc,
          ILogger log)
        {
           log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

           log.LogInformation(doc.FullDocument.ToString());
        }

        [FunctionName("InputBindingSample")]
        public static void InputBindingRun(
           [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
           [CosmosDBMongo("%vCoreDatabaseTrigger%", "%vCoreCollectionTrigger%", QueryString = "%queryString%")] List<BsonDocument> docs,
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
