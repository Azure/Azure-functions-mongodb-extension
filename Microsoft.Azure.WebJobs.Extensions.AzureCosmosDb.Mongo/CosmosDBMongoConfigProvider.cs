// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Binding;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoConfigProvider : IExtensionConfigProvider
    {
        private readonly ICosmosDBMongoBindingCollectorFactory _cosmosdbMongoBindingCollectorFactory;
        private readonly IConfiguration _configuration;
        private readonly INameResolver _nameResolver;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private ConcurrentDictionary<string, IMongoClient> CollectorCache { get; } = new ConcurrentDictionary<string, IMongoClient>();

        public CosmosDBMongoConfigProvider(ICosmosDBMongoBindingCollectorFactory cosmosdbMongoBindingCollectorFactory, IConfiguration configuration, INameResolver nameResolver, ILoggerFactory loggerFactory)
        {
            this._cosmosdbMongoBindingCollectorFactory = cosmosdbMongoBindingCollectorFactory;
            this._configuration = configuration;
            this._nameResolver = nameResolver;
            this._loggerFactory = loggerFactory;
            this._logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory(CosmosDBMongoConstant.AzureFunctionTelemetryCategory));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var triggerRule = context.AddBindingRule<CosmosDBMongoTriggerAttribute>();
            triggerRule.AddValidator(ValidateTriggerConnection);
            triggerRule.BindToTrigger(new CosmosDBMongoTriggerBindingProvider(this._nameResolver, this, this._loggerFactory));

            var bindingRule = context.AddBindingRule<CosmosDBMongoAttribute>();
            bindingRule.AddValidator(ValidateConnection);

            bindingRule.BindToCollector<OpenType.Poco>(typeof(CosmosDBMongoBindingCollectorBuilder<>), this, this._loggerFactory);
            bindingRule.BindToInput<IEnumerable<OpenType.Poco>>(typeof(CosmosDBMongoBindingEnumerableBuilder<>), this);
            bindingRule.BindToInput<List<OpenType.Poco>>(typeof(CosmosDBMongoBindingListBuilder<>), this);
            bindingRule.WhenIsNull(nameof(CosmosDBMongoAttribute.DatabaseName))
                .BindToInput(attribute =>
                {
                    return _cosmosdbMongoBindingCollectorFactory.CreateClient(ResolveConnectionString(attribute.ConnectionStringSetting));
                });
            bindingRule.WhenIsNull(nameof(CosmosDBMongoAttribute.CollectionName)).WhenIsNotNull(nameof(CosmosDBMongoAttribute.DatabaseName))
                .BindToInput(attribute =>
                {
                    return _cosmosdbMongoBindingCollectorFactory.CreateClient(ResolveConnectionString(attribute.ConnectionStringSetting)).GetDatabase(attribute.DatabaseName);
                });
            bindingRule.WhenIsNotNull(nameof(CosmosDBMongoAttribute.CollectionName)).WhenIsNotNull(nameof(CosmosDBMongoAttribute.DatabaseName))
                .BindToInput(attribute =>
                {
                    return _cosmosdbMongoBindingCollectorFactory.CreateClient(ResolveConnectionString(attribute.ConnectionStringSetting))
                        .GetDatabase(attribute.DatabaseName)
                        .GetCollection<BsonDocument>(attribute.CollectionName);
                });
        }

        internal CosmosDBMongoTriggerContext CreateTriggerContext(CosmosDBMongoTriggerAttribute attribute)
        {
            IMongoClient client = GetService(attribute.ConnectionStringSetting, attribute.DatabaseName, attribute.CollectionName);

            return new CosmosDBMongoTriggerContext
            {
                MongoClient = client,
                ResolvedAttribute = attribute,
            };
        }

        internal IMongoClient GetService(string connectionString, string databaseName, string collectionName)
        {
            string cacheKey = BuildCacheKey(connectionString, databaseName, collectionName);
            return CollectorCache.GetOrAdd(cacheKey, (c) => this._cosmosdbMongoBindingCollectorFactory.CreateClient(connectionString));
        }

        internal void ValidateConnection(CosmosDBMongoAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(ResolveConnectionString(attribute.ConnectionStringSetting)))
            {
                string attributeProperty = $"{nameof(CosmosDBMongoAttribute)}.{nameof(CosmosDBMongoAttribute.ConnectionStringSetting)}";
                throw new InvalidOperationException(
                    $"Connection string must be set via the {attributeProperty} property.");
            }
        }

        internal void ValidateTriggerConnection(CosmosDBMongoTriggerAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(attribute.ConnectionStringSetting))
            {
                string attributeProperty = $"{nameof(CosmosDBMongoTriggerAttribute)}.{nameof(CosmosDBMongoTriggerAttribute.ConnectionStringSetting)}";
                throw new InvalidOperationException(
                    $"Connection string must be set via the {attributeProperty} property.");
            }
        }

        internal IMongoClient GetService(string connection)
        {
            return _cosmosdbMongoBindingCollectorFactory.CreateClient(connection);
        }

        internal MongoCollectionReference ResolveCollectionReference(CosmosDBMongoAttribute attribute)
        {
            return new MongoCollectionReference(
                GetService(ResolveConnectionString(attribute.ConnectionStringSetting)),
                attribute.DatabaseName,
                attribute.CollectionName);
        }

        public string ResolveConnectionString(string connectionStringKey)
        {
            if (string.IsNullOrEmpty(connectionStringKey))
            {
                connectionStringKey = CosmosDBMongoConstant.DefaultConnectionStringKey;
            }

            string connection = _configuration.GetConnectionString(connectionStringKey);
            if (string.IsNullOrEmpty(connection))
            {
                connection = _configuration.GetValue<string>(connectionStringKey);
            }
            if (string.IsNullOrEmpty(connection))
            {
                connection = _configuration.GetWebJobsConnectionString(connectionStringKey);
            }
            if (string.IsNullOrEmpty(connection))
            {
                throw new InvalidOperationException($"Connection configuration '{connectionStringKey}' does not exist. " +
                                    $"Make sure that it is a defined App Setting or environment variable.");
            }
            return connection;
        }

        private static string BuildCacheKey(string connectionString, string databaseName, string collectionName) => $"{connectionString}|{databaseName}|{collectionName}";
    }
}