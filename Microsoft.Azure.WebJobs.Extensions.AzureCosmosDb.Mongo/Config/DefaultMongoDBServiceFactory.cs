// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Config
{
    /// <summary>
    /// Default factory for creating MongoDB clients with support for both connection string
    /// and Azure Entra ID (Managed Identity) authentication.
    /// </summary>
    internal class DefaultMongoDBServiceFactory : ICosmosDBMongoBindingCollectorFactory
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;

        public DefaultMongoDBServiceFactory(IConfiguration configuration, AzureComponentFactory componentFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        }

        /// <summary>
        /// Creates a MongoClient based on the connection configuration.
        /// </summary>
        /// <param name="connectionName">The connection configuration name/key.</param>
        /// <returns>An IMongoClient instance.</returns>
        public IMongoClient CreateClient(string connectionName)
        {
            var connectionInfo = ResolveConnectionInformation(connectionName);

            MongoClientSettings settings;

            if (connectionInfo.UsesConnectionString)
            {
                settings = MongoClientSettings.FromConnectionString(connectionInfo.ConnectionString);
            }
            else
            {
                settings = CreateSettingsFromCredential(connectionInfo.AccountEndpoint, connectionInfo.Credential);
            }

            settings.ApplicationName = CosmosDBMongoConstant.AzureFunctionApplicationName;

            return new MongoClient(settings);
        }

        /// <summary>
        /// Resolves connection information from configuration.
        /// </summary>
        private MongoDBConnectionInformation ResolveConnectionInformation(string connectionName)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                connectionName = CosmosDBMongoConstant.DefaultConnectionStringKey;
            }

            // Try to get as a simple connection string first
            string connectionString = _configuration.GetConnectionString(connectionName);
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = _configuration.GetValue<string>(connectionName);
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = _configuration.GetWebJobsConnectionString(connectionName);
            }

            // If we found a simple string value, it's a connection string
            if (!string.IsNullOrEmpty(connectionString))
            {
                return new MongoDBConnectionInformation(connectionString);
            }

            // Try to get as a structured configuration (for Entra ID)
            var section = _configuration.GetSection(connectionName);
            if (section.Exists())
            {
                string accountEndpoint = section.GetValue<string>("accountEndpoint");
                
                if (!string.IsNullOrEmpty(accountEndpoint))
                {
                    // This is an Entra ID configuration
                    TokenCredential credential = _componentFactory.CreateTokenCredential(section);
                    return new MongoDBConnectionInformation(accountEndpoint, credential);
                }
            }

            throw new InvalidOperationException(
                $"Connection configuration '{connectionName}' does not exist or is incomplete. " +
                $"Make sure that it is a defined App Setting, environment variable, or properly configured connection section.");
        }

        /// <summary>
        /// Creates MongoClientSettings from token credential for Entra ID authentication.
        /// </summary>
        private MongoClientSettings CreateSettingsFromCredential(string accountEndpoint, TokenCredential credential)
        {
            // Get an access token for Azure Cosmos DB
            var tokenRequestContext = new TokenRequestContext(
                new[] { "https://cosmos.azure.com/.default" });
            
            var token = credential.GetToken(tokenRequestContext, CancellationToken.None);

            // Parse the endpoint to extract server address
            string serverAddress;
            int port = 10255; // Default CosmosDB Mongo port

            if (accountEndpoint.Contains(":"))
            {
                var parts = accountEndpoint.Split(':');
                serverAddress = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
                {
                    port = parsedPort;
                }
            }
            else
            {
                serverAddress = accountEndpoint;
            }

            // Create MongoClientSettings with token authentication
            var settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(serverAddress, port),
                UseTls = true,
                Credential = MongoCredential.CreatePlainCredential(
                    "$external",
                    "$externalUser", 
                    token.Token)
            };

            return settings;
        }
    }
}
