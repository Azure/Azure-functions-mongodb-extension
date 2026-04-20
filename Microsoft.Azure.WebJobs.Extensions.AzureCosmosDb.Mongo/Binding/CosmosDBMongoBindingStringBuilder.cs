// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoBindingStringBuilder : IAsyncConverter<CosmosDBMongoAttribute, string>
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;

        public CosmosDBMongoBindingStringBuilder(CosmosDBMongoConfigProvider configProvider)
        {
            this._configProvider = configProvider;
        }

        public async Task<string> ConvertAsync(CosmosDBMongoAttribute attribute, CancellationToken cancellationToken)
        {
            MongoCollectionReference reference = this._configProvider.ResolveCollectionReference(attribute);

            IMongoCollection<BsonDocument> collection = reference.client
                .GetDatabase(reference.databaseName)
                .GetCollection<BsonDocument>(reference.collectionName);

            BsonDocument filter = new BsonDocument();
            if (!string.IsNullOrEmpty(attribute.QueryString))
            {
                filter = BsonDocument.Parse(attribute.QueryString);
            }

            List<BsonDocument> results = await collection.Find(filter).ToListAsync(cancellationToken);

            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
            var array = new BsonArray(results);
            return array.ToJson(jsonSettings);
        }
    }
}
