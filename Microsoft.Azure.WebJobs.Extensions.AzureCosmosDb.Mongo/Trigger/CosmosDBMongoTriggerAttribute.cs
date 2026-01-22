// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Description;
using MongoDB.Driver;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public enum MonitorLevel
    {
        Collection,
        Database,
        Cluster
    }

    /// <summary>
    /// Defines the [CosmosDBMongoTrigger] attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class CosmosDBMongoTriggerAttribute : Attribute
    {
        /// <summary>
        /// Triggers an event when changes occur on a monitored collection.
        /// </summary>
        /// <param name="databaseName">Name of the database to monitor for changes.</param>
        /// <param name="collectionName">Name of the collection to monitor for changes.</param>
        public CosmosDBMongoTriggerAttribute(string databaseName, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                TriggerLevel = MonitorLevel.Cluster;
                return;
            }
            DatabaseName = databaseName;

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                TriggerLevel = MonitorLevel.Database;
                return;
            }

            TriggerLevel = MonitorLevel.Collection;
            CollectionName = collectionName;
        }

        /// <summary>
        /// The Id of trigger function.
        /// </summary>
        public string FunctionId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the database to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        [AutoResolve]
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the collection to which the parameter applies. 
        /// May include binding parameters.
        /// </summary>
        [AutoResolve]
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Optional.
        /// Only applies to output bindings.
        /// If true, the database and collection will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// A string value indicating the app setting to use as the CosmosDB connection string.
        /// </summary>
        [AutoResolve]
        public string ConnectionStringSetting { get; set; }

        /// <summary>
        /// The monitored level of trigger
        /// </summary>
        public MonitorLevel TriggerLevel { get; set; } = MonitorLevel.Collection;

        /// <summary>
        /// Database name for the lease collection.
        /// If not specified, defaults to the monitored database name.
        /// </summary>
        [AutoResolve]
        public string LeaseDatabaseName { get; set; }

        /// <summary>
        /// Collection name for the lease collection.
        /// If not specified, defaults to "leases".
        /// </summary>
        [AutoResolve]
        public string LeaseCollectionName { get; set; }

        /// <summary>
        /// Connection string for the lease cluster.
        /// If not specified, defaults to the monitored cluster connection string.
        /// </summary>
        [AutoResolve]
        public string LeaseConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the authentication method to connect to cluster.
        /// Could be NativeAuth (default) or MicrosoftEntraID.
        /// </summary>
        public AuthMethod AuthMethod { get; set; } = AuthMethod.NativeAuth;

        /// <summary>
        /// Gets or sets the Azure AD tenant ID for Entra ID authentication.
        /// </summary>
        [AutoResolve]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the client ID for User-assigned Managed Identity.
        /// Leave empty to use System-assigned Managed Identity.
        /// </summary>
        [AutoResolve]
        public string ManagedIdentityClientId { get; set; }

        // /// <summary>
        // /// Gets or sets the Application (Client) ID for Service Principal authentication.
        // /// Must be used together with ClientSecretSetting.
        // /// </summary>
        // [AutoResolve]
        // public string ClientId { get; set; }

        // /// <summary>
        // /// Gets or sets the app setting name containing the Client Secret for Service Principal authentication.
        // /// Must be used together with ClientId.
        // /// </summary>
        // public string ClientSecretSetting { get; set; }
    }

    public class CosmosDBMongoTriggerContext
    {
        public IMongoClient MongoClient { get; set; }

        public CosmosDBMongoTriggerAttribute ResolvedAttribute { get; set; }
    }
}