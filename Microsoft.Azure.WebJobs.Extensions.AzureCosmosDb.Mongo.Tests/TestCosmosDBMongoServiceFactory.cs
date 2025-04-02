using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    internal class TestCosmosDBMongoServiceFactory
    {
        private MongoClient _service;

        public TestCosmosDBMongoServiceFactory(MongoClient service)
        {
            _service = service;
        }

        public MongoClient CreateClient(string connectionString)
        {
            return _service;
        }
    }
}
