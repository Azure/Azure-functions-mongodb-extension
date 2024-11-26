using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using System.Collections;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoConfigProvider : IExtensionConfigProvider
    {
        private readonly ICosmosDBMongoBindingCollectorFactory cosmosdbMongoBindingCollectorFactory;
        private readonly INameResolver nameResolver;

        private ConcurrentDictionary<string, MongoClient> CollectorCache { get; } = new ConcurrentDictionary<string, MongoClient>();

        public CosmosDBMongoConfigProvider(ICosmosDBMongoBindingCollectorFactory cosmosdbMongoBindingCollectorFactory, INameResolver nameResolver)
        {
            this.cosmosdbMongoBindingCollectorFactory = cosmosdbMongoBindingCollectorFactory;
            this.nameResolver = nameResolver;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var bindingRule = context.AddBindingRule<CosmosDBMongoAttribute>();
            bindingRule.AddValidator(ValidateConnection);
            bindingRule.BindToCollector<CosmosDBMongoBindingOpenType>(typeof(CosmosDBMongoBindingCollectorConverter<>), this);

            //bindingRule.BindToInput<MongoClient>(new CosmosDBMongoBindingClientConverter(this));
            //bindingRule.BindToInput<ParameterBindingData>((attr) => CreateParameterBindingData(attr));
            bindingRule.BindToInput<List<BsonDocument>>(typeof(CosmosDBMongoBindingListConverter<>), this);

            var triggerRule = context.AddBindingRule<CosmosDBMongoTriggerAttribute>();
            triggerRule.AddValidator(ValidateTriggerConnection);
            triggerRule.BindToTrigger(new CosmosDBMongoTriggerBindingProvider(this.nameResolver, this));
        }

        internal CosmosDBMongoContext CreateContext(CosmosDBMongoAttribute attribute)
        {
            MongoClient client = GetService(attribute.ConnectionStringSetting, attribute.DatabaseName, attribute.CollectionName);

            return new CosmosDBMongoContext
            {
                MongoClient = client,
                ResolvedAttribute = attribute,
            };
        }

        internal CosmosDBMongoTriggerContext CreateContextTrigger(CosmosDBMongoTriggerAttribute attribute)
        {
            MongoClient client = GetService(attribute.ConnectionStringSetting, attribute.DatabaseName, attribute.CollectionName);

            return new CosmosDBMongoTriggerContext
            {
                MongoClient = client,
                ResolvedAttribute = attribute,
            };
        }

        internal ParameterBindingData CreateParameterBindingData(CosmosDBMongoAttribute attribute)
        {
            var cosmosDBMongoDetails = new CosmosDBMongoParameterBindingDataContent(attribute);
            var cosmosDBMongoDetailsBinaryData = new BinaryData(cosmosDBMongoDetails);
            var parameterBindingData = new ParameterBindingData("1.0", "CosmosDBMongo", cosmosDBMongoDetailsBinaryData, "application/json");

            return parameterBindingData;
        }

        internal MongoClient GetService(string connectionString, string databaseName, string collectionName)
        {
            string cacheKey = BuildCacheKey(connectionString, databaseName, collectionName);
            return CollectorCache.GetOrAdd(cacheKey, (c) => this.cosmosdbMongoBindingCollectorFactory.CreateClient(connectionString));
        }

        internal void ValidateConnection(CosmosDBMongoAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(attribute.ConnectionStringSetting))
            {
                string attributeProperty = $"{nameof(CosmosDBMongoAttribute)}.{nameof(CosmosDBMongoAttribute.ConnectionStringSetting)}";
                throw new InvalidOperationException(
                    $"The mongo connection string must be set via the {attributeProperty} property.");
            }
        }

        internal void ValidateTriggerConnection(CosmosDBMongoTriggerAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(attribute.ConnectionStringSetting))
            {
                string attributeProperty = $"{nameof(CosmosDBMongoTriggerAttribute)}.{nameof(CosmosDBMongoTriggerAttribute.ConnectionStringSetting)}";
                throw new InvalidOperationException(
                    $"The mongo connection string must be set via the {attributeProperty} property.");
            }
        }

        private static string BuildCacheKey(string connectionString, string databaseName, string collectionName) => $"{connectionString}|{databaseName}|{collectionName}";

        private class CosmosDBMongoBindingOpenType : OpenType.Poco
        {
            public override bool IsMatch(Type type, OpenTypeMatchContext context)
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return false;
                }

                if (type.FullName == "System.Object")
                {
                    return true;
                }

                return base.IsMatch(type, context);
            }
        }

        private class CosmosDBMongoParameterBindingDataContent
        {
            public CosmosDBMongoParameterBindingDataContent(CosmosDBMongoAttribute attribute)
            {
                DatabaseName = attribute.DatabaseName;
                CollectionName = attribute.CollectionName;
                CreateIfNotExists = attribute.CreateIfNotExists;
                ConnectionStringSetting = attribute.ConnectionStringSetting;
            }

            public string DatabaseName { get; set; }

            public string CollectionName { get; set; }

            public bool CreateIfNotExists { get; set; }

            public string ConnectionStringSetting { get; set; }
        }
    }
}