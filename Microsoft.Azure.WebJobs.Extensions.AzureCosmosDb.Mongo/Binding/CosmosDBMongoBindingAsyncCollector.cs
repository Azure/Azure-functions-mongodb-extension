// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

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

                await UpsertDocument(this._reference, item, cancellationToken);
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

            // Out-of-process workers (Node, Python, Java, ...) deliver the binding payload as
            // Newtonsoft.Json.Linq.JObject or a JSON string. BSON's reflection-based serializer
            // does not know about JObject/JValue, so it would write the container's internal
            // members (`_t`, `_v`) as if they were document fields. For those cases convert to
            // a BsonDocument explicitly and write through an IMongoCollection<BsonDocument>.
            if (TryConvertToBsonDocument(doc, out BsonDocument bsonDoc))
            {
                var bsonCollection = database.GetCollection<BsonDocument>(reference.collectionName);
                try
                {
                    if (bsonDoc.TryGetValue("_id", out BsonValue idValue))
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", idValue);
                        var options = new ReplaceOptions { IsUpsert = true };
                        await bsonCollection.ReplaceOneAsync(filter, bsonDoc, options, cancellationToken);
                    }
                    else
                    {
                        await bsonCollection.InsertOneAsync(bsonDoc, null, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(Events.OnBindingDataError, $"Error upserting BsonDocument: {ex.Message}");
                }

                return;
            }

            // .NET in-process / isolated workers deliver a strongly-typed POCO. Keep the
            // original typed-collection path so existing serialization (BsonClassMap +
            // [BsonElement] attributes) is preserved.
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
                this._logger.LogError(Events.OnBindingDataError, $"Error upserting document: {ex.Message}");
            }
        }

        private static bool TryConvertToBsonDocument(T doc, out BsonDocument bsonDoc)
        {
            switch (doc)
            {
                case BsonDocument bd:
                    bsonDoc = bd;
                    return true;

                case JObject jObject:
                    bsonDoc = BsonDocument.Parse(jObject.ToString(Newtonsoft.Json.Formatting.None));
                    return true;

                case string s when !string.IsNullOrWhiteSpace(s):
                    bsonDoc = BsonDocument.Parse(s);
                    return true;

                default:
                    bsonDoc = null;
                    return false;
            }
        }
    }
}
