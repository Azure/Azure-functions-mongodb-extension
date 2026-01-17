// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample
{
    public static class Sample
    {
        #region Entra ID Authentication Samples

        /// <summary>
        /// Sample 1: Entra ID with System-assigned Managed Identity (or local dev credentials)
        /// - In Azure: Uses the Function App's System-assigned Managed Identity
        /// - Locally: Uses Visual Studio, Azure CLI, or other DefaultAzureCredential sources
        /// </summary>
        [FunctionName("EntraIdAuthSample")]
        public static void EntraIdAuthRun(
            [TimerTrigger("*/30 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo(
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%")] IMongoClient client,
            ILogger log)
        {
            log.LogInformation($"Entra ID Auth Sample (System MI) executed at: {DateTime.Now}");

            try
            {
                var databases = client.ListDatabaseNames().ToList();
                log.LogInformation($"Connected via Entra ID. Found {databases.Count} database(s):");
                
                foreach (var dbName in databases)
                {
                    log.LogInformation($"  Database: {dbName}");
                    var db = client.GetDatabase(dbName);
                    var collections = db.ListCollectionNames().ToList();
                    
                    foreach (var collName in collections)
                    {
                        log.LogInformation($"    - Collection: {collName}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to connect with Entra ID authentication");
            }
        }

        /// <summary>
        /// Sample 2: Entra ID with User-assigned Managed Identity
        /// - Requires ManagedIdentityClientId to be set to the Client ID of the User-assigned MI
        /// - Useful when you have multiple identities or need cross-subscription access
        /// </summary>
        [FunctionName("EntraIdUserAssignedMISample")]
        public static void EntraIdUserAssignedMIRun(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // Every 5 minutes
            [CosmosDBMongo(
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%",
                ManagedIdentityClientId = "%ManagedIdentityClientId%")] IMongoClient client,
            ILogger log)
        {
            log.LogInformation($"Entra ID Auth Sample (User-assigned MI) executed at: {DateTime.Now}");

            try
            {
                var databases = client.ListDatabaseNames().ToList();
                log.LogInformation($"Connected via User-assigned MI. Found {databases.Count} database(s).");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to connect with User-assigned Managed Identity");
            }
        }

        /// <summary>
        /// Sample 3: Entra ID with Service Principal (Client ID + Client Secret)
        /// - Requires ClientId (Application ID) and ClientSecretSetting
        /// - ClientSecretSetting points to an app setting containing the secret
        /// - Useful for automated scenarios, CI/CD, or cross-tenant access
        /// </summary>
        [FunctionName("EntraIdServicePrincipalSample")]
        public static void EntraIdServicePrincipalRun(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // Every 5 minutes
            [CosmosDBMongo(
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%",
                ClientId = "%ServicePrincipalClientId%",
                ClientSecretSetting = "ServicePrincipalClientSecret")] IMongoClient client,
            ILogger log)
        {
            log.LogInformation($"Entra ID Auth Sample (Service Principal) executed at: {DateTime.Now}");

            try
            {
                var databases = client.ListDatabaseNames().ToList();
                log.LogInformation($"Connected via Service Principal. Found {databases.Count} database(s).");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to connect with Service Principal");
            }
        }

        #endregion

        #region Client Binding Samples

        /// <summary>
        /// Sample: Get MongoClient with native authentication (default)
        /// </summary>
        [FunctionName("ClientBindingSample")]
        public static void ClientBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo(ConnectionStringSetting = "MongoDBConnection")] IMongoClient client,
            ILogger log)
        {
            var documents = client.GetDatabase("TestDatabase")
                .GetCollection<BsonDocument>("TestCollection")
                .Find(new BsonDocument())
                .ToList();

            foreach (BsonDocument d in documents)
            {
                log.LogInformation(d.ToString());
            }
        }

        /// <summary>
        /// Sample: Get MongoClient with Microsoft Entra ID authentication
        /// Requires .NET 8.0 or later
        /// Connection string should NOT contain username/password
        /// e.g., "mongodb+srv://your-cluster.mongocluster.cosmos.azure.com/?tls=true"
        /// </summary>
        [FunctionName("ClientBindingWithEntraIdSample")]
        public static void ClientBindingWithEntraIdRun(
            [TimerTrigger("*/30 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo(
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%")] IMongoClient client,
            ILogger log)
        {
            log.LogInformation($"Entra ID Auth Sample executed at: {DateTime.Now}");

            try
            {
                // List databases to verify connection
                var databases = client.ListDatabaseNames().ToList();
                log.LogInformation($"Connected via Entra ID. Found {databases.Count} databases:");
                foreach (var db in databases)
                {
                    log.LogInformation($"  - {db}");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to connect with Entra ID authentication");
            }
        }

        #endregion

        #region Output Binding Samples

        /// <summary>
        /// Sample: Output binding to insert documents
        /// </summary>
        [FunctionName("OutputBindingSample")]
        public static async Task OutputBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%DatabaseName%", "%CollectionName%",
                ConnectionStringSetting = "MongoDBConnection")] IAsyncCollector<TestClass> collector,
            ILogger log)
        {
            log.LogInformation($"Output binding sample executed at: {DateTime.Now}");

            TestClass item = new TestClass()
            {
                id = Guid.NewGuid().ToString(),
                SomeData = "some random data"
            };

            await collector.AddAsync(item);
        }

        /// <summary>
        /// Sample: Output binding with Entra ID authentication
        /// </summary>
        [FunctionName("OutputBindingWithEntraIdSample")]
        public static async Task OutputBindingWithEntraIdRun(
            [TimerTrigger("*/30 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%DatabaseName%", "%CollectionName%",
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%")] IAsyncCollector<TestClass> collector,
            ILogger log)
        {
            log.LogInformation($"Output binding with Entra ID executed at: {DateTime.Now}");

            TestClass item = new TestClass()
            {
                id = Guid.NewGuid().ToString(),
                SomeData = "data inserted via Entra ID auth"
            };

            await collector.AddAsync(item);
        }

        #endregion

        #region Input Binding Samples

        /// <summary>
        /// Sample: Input binding to query documents
        /// </summary>
        [FunctionName("InputBindingSample")]
        public static void InputBindingRun(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [CosmosDBMongo("%DatabaseName%", "%CollectionName%",
                ConnectionStringSetting = "MongoDBConnection",
                QueryString = "%QueryString%")] List<BsonDocument> docs,
            ILogger log)
        {
            log.LogInformation($"Input binding sample executed at: {DateTime.Now}");

            foreach (var doc in docs)
            {
                log.LogInformation(doc.ToString());
            }
        }

        #endregion

        #region Trigger Samples

        /// <summary>
        /// Sample: Change stream trigger to monitor collection changes
        /// </summary>
        [FunctionName("TriggerSample")]
        public static void TriggerRun(
            [CosmosDBMongoTrigger("TestDatabase", "TestCollection",
                ConnectionStringSetting = "MongoDBConnection",
                LeaseDatabaseName = "TestDatabase",
                LeaseCollectionName = "leases")] ChangeStreamDocument<BsonDocument> doc,
            ILogger log)
        {
            log.LogInformation($"Change detected at: {DateTime.Now}");
            log.LogInformation(doc.FullDocument.ToString());
        }

        /// <summary>
        /// Sample: Change stream trigger with Entra ID authentication
        /// </summary>
        [FunctionName("TriggerWithEntraIdSample")]
        public static void TriggerWithEntraIdRun(
            [CosmosDBMongoTrigger("TestDatabase", "TestCollection",
                ConnectionStringSetting = "EntraIdConnection",
                AuthMethod = AuthMethod.MicrosoftEntraID,
                TenantId = "%TenantId%",
                LeaseDatabaseName = "TestDatabase",
                LeaseCollectionName = "leases")] ChangeStreamDocument<BsonDocument> doc,
            ILogger log)
        {
            log.LogInformation($"Change detected via Entra ID at: {DateTime.Now}");
            log.LogInformation(doc.FullDocument.ToString());
        }

        #endregion
    }

    public class TestClass
    {
        public string id { get; set; }
        public string SomeData { get; set; }
    }
}
