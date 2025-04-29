// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Description;
using MongoDB.Driver;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Attribute used to bind to an Azure CosmosDB Mongo vCore account.
    /// </summary>
    /// <remarks>
    /// The method parameter type can be one of the following:
    /// <list type="bullet">
    /// <item><description><see cref="ICollector{T}"/></description></item>
    /// <item><description><see cref="IAsyncCollector{T}"/></description></item>
    /// <item><description>out T</description></item>
    /// <item><description>out T[]</description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class CosmosDBMongoAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public CosmosDBMongoAttribute()
        {
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The vcore database name.</param>
        /// <param name="collectionName">The vcore collection name.</param>
        public CosmosDBMongoAttribute(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The vcore database name.</param>
        public CosmosDBMongoAttribute(string databaseName)
        {
            DatabaseName = databaseName;
        }

        /// <summary>
        /// The Id of binding function.
        /// </summary>
        public string FunctionId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the database to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        [AutoResolve]
        public string DatabaseName { get; private set; }

        /// <summary>
        /// The name of the collection to which the parameter applies. 
        /// May include binding parameters.
        /// </summary>
        [AutoResolve]
        public string CollectionName { get; private set; }

        /// <summary>
        /// Optional.
        /// Only applies to output bindings.
        /// If true, the database and collection will be automatically created if they do not exist.
        /// Default is false
        /// </summary>
        public bool CreateIfNotExists { get; set; } = false;

        /// <summary>
        /// A string value indicating the app setting to use as the CosmosDB connection string.
        /// </summary>
        [AutoResolve]
        public string ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets a mongo query expression for an input binding to execute on the collection and produce results.
        /// May include binding parameters.
        /// </summary>
        [AutoResolve]
        public string QueryString { get; set; }
    }

    public class CosmosDBMongoContext
    {
        public IMongoClient MongoClient { get; set; }

        public CosmosDBMongoAttribute ResolvedAttribute { get; set; }
    }
}