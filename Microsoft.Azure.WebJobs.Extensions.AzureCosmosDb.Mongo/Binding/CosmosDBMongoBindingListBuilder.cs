using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoBindingListBuilder<T> : IAsyncConverter<CosmosDBMongoAttribute, List<T>>
        where T : class
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;
        private readonly ILogger _logger;

        public CosmosDBMongoBindingListBuilder(CosmosDBMongoConfigProvider configProvider, ILogger logger)
        {
            this._configProvider = configProvider;
            this._logger = logger;
        }

        public async Task<List<T>> ConvertAsync(CosmosDBMongoAttribute attribute, CancellationToken cancellationToken)
        {
            CosmosDBMongoContext context = this._configProvider.CreateContext(attribute);

            List<T> finalResults = new List<T>();

            try{
                IMongoCollection<T> collection = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName).GetCollection<T>(context.ResolvedAttribute.CollectionName);

                BsonDocument filter = null;
                if (!string.IsNullOrEmpty(attribute.QueryString))
                {
                    filter = BsonDocument.Parse(attribute.QueryString);
                }

                this._logger.LogDebug(Events.OnBindingInputQuery, $"Querying collection {context.ResolvedAttribute.CollectionName} with filter {filter}.");
                return await collection.Find(filter).ToListAsync();
            }
            catch (System.Exception ex)
            {
                this._logger.LogError(Events.OnBindingInputQueryError, $"Error querying collection {context.ResolvedAttribute.CollectionName}: {ex.Message}");
                throw;
            }
        }
    }
}