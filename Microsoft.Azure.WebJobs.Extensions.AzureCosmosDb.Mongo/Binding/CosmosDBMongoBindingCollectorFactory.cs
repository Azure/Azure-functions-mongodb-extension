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
            return CreateClient(connectionString, tenantId: null);
        }

        public IMongoClient CreateClient(
            string connectionString, 
            string tenantId, 
            string managedIdentityClientId = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            // Auth method is auto-detected: TenantId present = Entra ID, otherwise NativeAuth
            var authHandler = AuthHandlerFactory.Create(tenantId, managedIdentityClientId);
            var settings = authHandler.ConfigureAuth(connectionString);

            return new MongoClient(settings);
        }
    }
}