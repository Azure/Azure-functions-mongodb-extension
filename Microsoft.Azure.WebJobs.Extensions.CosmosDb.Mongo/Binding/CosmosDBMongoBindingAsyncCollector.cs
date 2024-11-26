using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoBindingAsyncCollector<T> : IAsyncCollector<T>
    {
        private CosmosDBMongoContext mongoContext;

        public CosmosDBMongoBindingAsyncCollector(CosmosDBMongoContext mongoContext) => this.mongoContext = mongoContext;

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this.mongoContext.ResolvedAttribute.CreateIfNotExists)
            {
                InitializeCollection(this.mongoContext);
            }

            await UpsertDocument(this.mongoContext, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // no-op
            return Task.FromResult(0);
        }

        private static void InitializeCollection(CosmosDBMongoContext context)
        {
            var database = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName);

            var collectionExists = database.ListCollectionNames().ToList().Contains(context.ResolvedAttribute.CollectionName);
            if (!collectionExists)
            {
                Console.WriteLine($"creating collection {context.ResolvedAttribute.CollectionName}");
                database.CreateCollection(context.ResolvedAttribute.CollectionName);
            }
        }

        private static async Task UpsertDocument(CosmosDBMongoContext context, T doc)
        {
            var database = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName);
            var collection = database.GetCollection<T>(context.ResolvedAttribute.CollectionName);

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
                    await collection.InsertOneAsync(doc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}