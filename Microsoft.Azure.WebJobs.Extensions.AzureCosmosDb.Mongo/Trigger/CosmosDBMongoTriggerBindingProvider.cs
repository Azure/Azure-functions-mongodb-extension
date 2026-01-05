// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;
        private readonly INameResolver _nameResolver;
        private readonly ILogger _logger;

        public CosmosDBMongoTriggerBindingProvider(INameResolver nameResolver, CosmosDBMongoConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            this._nameResolver = nameResolver;
            this._configProvider = configProvider;
            this._logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory(CosmosDBMongoConstant.AzureFunctionTelemetryCategory));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            
            var attribute = context.Parameter.GetCustomAttribute<CosmosDBMongoTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            
            string connectionName = attribute.ConnectionStringSetting;
            string functionId = context.Parameter.Member.Name;
            
            string databaseName = ResolveAttributeValue(attribute.DatabaseName);
            string collectionName = ResolveAttributeValue(attribute.CollectionName);
            
            var reference = new MongoCollectionReference(
                        _configProvider.GetService(connectionName),
                        databaseName,
                        collectionName);
            reference.functionId = functionId;
            
            // Resolve lease connection (defaults to monitored cluster connection)
            string leaseConnectionName = string.IsNullOrEmpty(attribute.LeaseConnectionStringSetting)
                ? connectionName
                : attribute.LeaseConnectionStringSetting;
            string leaseDatabaseName = ResolveAttributeValue(attribute.LeaseDatabaseName);
            if (string.IsNullOrEmpty(leaseDatabaseName))
            {
                throw new InvalidOperationException(
                    "LeaseDatabaseName is required. Please specify a database name for the lease collection.");
            }

            string leaseCollectionName = ResolveAttributeValue(attribute.LeaseCollectionName);
            if (string.IsNullOrEmpty(leaseCollectionName))
            {
                throw new InvalidOperationException(
                    "LeaseCollectionName is required. Please specify a collection name for the lease collection.");
            }
            
            reference.leaseClient = _configProvider.GetService(leaseConnectionName);
            reference.leaseConnectionStringSetting = string.IsNullOrEmpty(attribute.LeaseConnectionStringSetting) 
                ? attribute.ConnectionStringSetting 
                : attribute.LeaseConnectionStringSetting;
            reference.leaseDatabaseName = leaseDatabaseName;
            reference.leaseCollectionName = leaseCollectionName;
            
            return
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(context.Parameter,
                    reference,
                    this._logger));
        }

        private string ResolveAttributeValue(string value)
        {
            return this._nameResolver.Resolve(value) ?? value;
        }
    }
}