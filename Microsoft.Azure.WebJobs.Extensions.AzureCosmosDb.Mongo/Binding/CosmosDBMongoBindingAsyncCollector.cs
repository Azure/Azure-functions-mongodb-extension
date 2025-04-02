using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly ILogger _logger;

        private readonly CosmosDBMongoAttribute _attribute;
        private readonly MongoCollectionReference _reference;

        public CosmosDBMongoBindingAsyncCollector(CosmosDBMongoAttribute attribute, MongoCollectionReference reference, ILogger logger)
        {
            this._attribute = attribute;
            this._reference = reference;
            this._logger = logger;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                if (this._reference.createIfNotExists)
                {
                    await InitializeCollection(this._reference);
                }

                await UpsertDocument(this._reference, item);
                this._logger.LogDebug(Events.OnBindingDataAdded, "Document upserted successfully.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(Events.OnBindingDataError, $"Error upserting document: {ex.Message}");
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // no-op
            return Task.FromResult(0);
        }

        private async Task InitializeCollection(MongoCollectionReference reference)
        {
            if (reference.createIfNotExists)
            {
                await MongoUtility.CreateCollectionIfNotExistAsync(reference);
                this._logger.LogDebug(Events.OnIntializedCollection, $"Collection {reference.collectionName} created successfully.");
            }

            var database = reference.client.GetDatabase(reference.databaseName);
            // should throw error if _collection not exist and createifnotexists is false
            var collection = database.GetCollection<T>(reference.collectionName);
        }

        private async Task UpsertDocument(MongoCollectionReference reference, T doc, CancellationToken cancellationToken = default)
        {
            var database = reference.client.GetDatabase(reference.databaseName);
            var collection = database.GetCollection<T>(reference.collectionName);

            var idProperty = typeof(T).GetProperty("_id");

            try
            {
                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(doc);
                    var filter = Builders<T>.Filter.Eq("_id", idValue);
                    var update = Builders<T>.Update.Set("updatedAt", DateTime.UtcNow);
                    var options = new UpdateOptions { IsUpsert = true };

                    await collection.UpdateOneAsync(filter, update, options);
                }
                else
                {
                    await collection.InsertOneAsync(doc, null, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}