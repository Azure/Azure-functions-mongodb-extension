// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth;
using MongoDB.Driver;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingCollectorFactory : ICosmosDBMongoBindingCollectorFactory
    {
        public IMongoClient CreateClient(string connectionString)
        {
            return CreateClient(connectionString, AuthMethod.NativeAuth);
        }

        public IMongoClient CreateClient(
            string connectionString, 
            AuthMethod authMethod, 
            string tenantId = null, 
            string managedIdentityClientId = null)
            // string clientId = null,
            // string clientSecret = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var authHandler = AuthHandlerFactory.Create(authMethod, tenantId, managedIdentityClientId);
            var settings = authHandler.ConfigureAuth(connectionString);

            // Set the application name for telemetry
            settings.ApplicationName = CosmosDBMongoConstant.AzureFunctionApplicationName;

            return new MongoClient(settings);
        }
    }
}