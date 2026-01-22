// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public interface ICosmosDBMongoBindingCollectorFactory
    {
        IMongoClient CreateClient(string connectionString);

        IMongoClient CreateClient(
            string connectionString, 
            AuthMethod authMethod, 
            string tenantId = null, 
            string managedIdentityClientId = null);
            // string clientId = null,
            // string clientSecret = null);
    }
}