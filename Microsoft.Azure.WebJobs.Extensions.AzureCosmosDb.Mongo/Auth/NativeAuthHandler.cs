// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth
{
    public class NativeAuthHandler : IAuthHandler
    {
        public MongoClientSettings ConfigureAuth(string connectionString)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ApplicationName = CosmosDBMongoConstant.AzureFunctionApplicationName;
            return settings;
        }
    }
}