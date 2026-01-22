// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth
{
    public interface IAuthHandler
    {
        MongoClientSettings ConfigureAuth(string connectionString);
    }
}