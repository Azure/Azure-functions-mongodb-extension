using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class MongoCollectionReference
    {
        public IMongoClient client { get; }
        public string databaseName { get; }
        public string collectionName { get; }
        public bool createIfNotExists { get; }
        public string functionId { get; set; } = string.Empty;

        public MongoCollectionReference(IMongoClient client, string databaseName, string collectionName, bool createIfNotExists = true)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.createIfNotExists = createIfNotExists;
        }

        public MongoCollectionReference(CosmosDBMongoAttribute attribute)
        {
            this.client = client;
            this.databaseName = attribute.DatabaseName;
            this.collectionName = attribute.CollectionName;
            this.createIfNotExists = attribute.CreateIfNotExists;
        }
    }
}
