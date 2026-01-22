// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public interface ICosmosDBMongoBindingCollectorFactory
    {
        IMongoClient CreateClient(string connectionString);

        /// <summary>
        /// Creates a MongoDB client with auto-detected authentication.
        /// If tenantId is specified, uses Microsoft Entra ID authentication.
        /// Otherwise, uses native MongoDB authentication.
        /// </summary>
        IMongoClient CreateClient(
            string connectionString, 
            string tenantId, 
            string managedIdentityClientId = null);
    }
}