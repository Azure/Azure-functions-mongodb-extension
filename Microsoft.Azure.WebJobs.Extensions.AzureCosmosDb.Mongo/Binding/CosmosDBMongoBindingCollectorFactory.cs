// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Driver;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingCollectorFactory : ICosmosDBMongoBindingCollectorFactory
    {
        public IMongoClient CreateClient(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ApplicationName = CosmosDBMongoConstant.AzureFunctionApplicationName;

            return new MongoClient(settings);
        }
    }
}