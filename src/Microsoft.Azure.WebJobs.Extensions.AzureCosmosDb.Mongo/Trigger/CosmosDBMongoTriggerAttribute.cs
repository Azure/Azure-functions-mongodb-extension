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

        // /// <summary>
        // /// The id of trigger function.
        // /// </summary>
        // public string FunctionId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the database to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the collection to which the parameter applies. 
        /// May include binding parameters.
        /// </summary>
        public string CollectionName { get; set; } = string.Empty ;

        /// <summary>
        /// Optional.
        /// Only applies to output bindings.
        /// If true, the database and collection will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// A string value indicating the app setting to use as the CosmosDB connection string.
        /// </summary>
        public string ConnectionStringSetting { get; set; }


        /// <summary>
        /// The monitored level of trigger
        /// </summary>
        public MonitorLevel TriggerLevel { get; set; } = MonitorLevel.Collection;
    }

    public class CosmosDBMongoTriggerContext
    {
        public IMongoClient MongoClient { get; set; }

        public CosmosDBMongoTriggerAttribute ResolvedAttribute { get; set; }
    }
}