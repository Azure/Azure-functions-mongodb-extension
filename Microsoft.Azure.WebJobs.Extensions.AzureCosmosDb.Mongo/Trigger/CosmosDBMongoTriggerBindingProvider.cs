// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host;
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
            {
                throw new ArgumentNullException(nameof(context));
            }

            ParameterInfo parameter = context.Parameter;
            CosmosDBMongoTriggerAttribute attribute = parameter.GetCustomAttribute<CosmosDBMongoTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string connectionString = _configProvider.ResolveConnectionString(attribute.ConnectionStringSetting);
            string functionId = parameter.Member.Name;

            string databaseName = ResolveAttributeValue(attribute.DatabaseName);
            string collectionName = ResolveAttributeValue(attribute.CollectionName);

            // Resolve authentication settings for monitored cluster
            // Auth method is auto-detected: TenantId present = Entra ID, otherwise NativeAuth
            string tenantId = ResolveAttributeValue(attribute.TenantId);
            string managedIdentityClientId = ResolveAttributeValue(attribute.ManagedIdentityClientId);

            var reference = new MongoCollectionReference(
                        _configProvider.GetServiceWithAuth(
                            connectionString,
                            tenantId,
                            managedIdentityClientId),
                        databaseName,
                        collectionName);
            reference.functionId = functionId;

            // Resolve lease connection string (defaults to monitored cluster connection string)
            string leaseConnectionString = string.IsNullOrEmpty(attribute.LeaseConnectionStringSetting)
                ? connectionString
                : _configProvider.ResolveConnectionString(attribute.LeaseConnectionStringSetting);

            // Resolve lease database name (defaults to monitored database name)
            string leaseDatabaseName = ResolveAttributeValue(attribute.LeaseDatabaseName);
            if (string.IsNullOrEmpty(leaseDatabaseName))
            {
                leaseDatabaseName = databaseName;
            }

            // Resolve lease collection name (defaults to "leases")
            string leaseCollectionName = ResolveAttributeValue(attribute.LeaseCollectionName);
            if (string.IsNullOrEmpty(leaseCollectionName))
            {
                leaseCollectionName = "leases";
            }

            // Resolve lease authentication settings
            bool isSameCluster = string.IsNullOrEmpty(attribute.LeaseConnectionStringSetting) ||
                                 attribute.LeaseConnectionStringSetting == attribute.ConnectionStringSetting;
            
            string leaseTenantId = ResolveAttributeValue(attribute.LeaseTenantId);
            if (string.IsNullOrEmpty(leaseTenantId) && isSameCluster)
            {
                // Only inherit TenantId if using the same cluster
                leaseTenantId = tenantId;
            }
            // If different cluster and LeaseTenantId not specified, leaseTenantId remains null
            
            string leaseManagedIdentityClientId = ResolveAttributeValue(attribute.LeaseManagedIdentityClientId);
            if (string.IsNullOrEmpty(leaseManagedIdentityClientId) && isSameCluster)
            {
                // Only inherit ManagedIdentityClientId if using the same cluster
                leaseManagedIdentityClientId = managedIdentityClientId;
            }

            // Create lease client with appropriate authentication (auto-detected from LeaseTenantId)
            reference.leaseClient = _configProvider.GetServiceWithAuth(
                leaseConnectionString,
                leaseTenantId,
                leaseManagedIdentityClientId);
            reference.leaseConnectionStringSetting = string.IsNullOrEmpty(attribute.LeaseConnectionStringSetting)
                ? attribute.ConnectionStringSetting
                : attribute.LeaseConnectionStringSetting;
            reference.leaseDatabaseName = leaseDatabaseName;
            reference.leaseCollectionName = leaseCollectionName;
            reference.leaseTenantId = leaseTenantId;
            reference.leaseManagedIdentityClientId = leaseManagedIdentityClientId;

            return
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(parameter,
                    reference,
                    this._logger));
        }

        /// <summary>
        /// Resolves attribute values using the INameResolver.
        /// Uses the same approach as the official CosmosDB Trigger:
        /// INameResolver.ResolveWholeString() handles %xxx% syntax automatically.
        /// </summary>
        private string ResolveAttributeValue(string attributeValue)
        {
            return _nameResolver.ResolveWholeString(attributeValue) ?? attributeValue;
        }
    }
}