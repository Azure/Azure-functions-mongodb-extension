using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoContext
    {
        public MongoClient MongoClient { get; set; }

        public CosmosDBMongoAttribute ResolvedAttribute { get; set; }
    }

    public class CosmosDBMongoTriggerContext
    {
        public MongoClient MongoClient { get; set; }

        public CosmosDBMongoTriggerAttribute ResolvedAttribute { get; set; }
    }
}
