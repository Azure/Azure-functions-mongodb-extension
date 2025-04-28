// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    [TestClass]
    [TestCategory("EmulatorRequired")]
    public class CosmosDBMongoEndToEndTests
    {
        private const string DatabaseName = "TestDatabase";
        private const string CollectionName = "TestCollection";
        private string ConnectionString = Environment.GetEnvironmentVariable("CosmosDBMongoReal")!;
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        [TestInitialize]
        public async Task Setup()
        {
            await new MongoClient(ConnectionString)
                    .DropDatabaseAsync(DatabaseName);

            await new MongoClient(ConnectionString)
                .GetDatabase(DatabaseName).CreateCollectionAsync(CollectionName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await new MongoClient(ConnectionString)
                .DropDatabaseAsync(DatabaseName);
        }

        [TestMethod]
        public async Task TestTrigger()
        {
            IHost host = null;

            try
            {
                host = await CreateAndStartHostAsync();

                var db = new MongoClient(ConnectionString)
                    .GetDatabase(DatabaseName);
                var coll = db.GetCollection<BsonDocument>(CollectionName);

                Console.WriteLine("Delay completed, preparing to insert documents");

                _loggerProvider.ClearAllLogMessages();

                for (int i = 0; i < 3; i++)
                {
                    await coll.InsertOneAsync(new BsonDocument()
                    {
                        {"_id", i},
                        { "timestamp", DateTime.UtcNow.ToString("o") },
                        { "testId", Guid.NewGuid().ToString() }
                    });

                    await Task.Delay(1000);
                }

                await WaitForPredicate(
                    () => {
                        return _loggerProvider.GetAllLogMessages().Count(m => m.FormattedMessage != null && m.FormattedMessage.Contains("Doc triggered")) == 3;
                    });
            }
            finally
            {
                int docTriggeredCount = _loggerProvider.GetAllLogMessages().Count(m => m.FormattedMessage != null && m.FormattedMessage.Contains("Doc triggered"));
                Console.WriteLine($"Doc triggered count: {docTriggeredCount}");
                Assert.AreEqual(3, docTriggeredCount);
                Console.WriteLine("Final log status:");
                var allLogs = _loggerProvider.GetAllLogMessages().ToList();
                foreach (var log in allLogs)
                {
                    Console.WriteLine($"Log: {log.FormattedMessage}");
                }

                if (host != null)
                {
                    await host.StopAsync();
                }
            }
        }

        private async Task<IHost> CreateAndStartHostAsync()
        {
            ExplicitTypeLocator typeLocator = new ExplicitTypeLocator(typeof(TestFunctions));
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder.AddCosmosDBMongo();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(typeLocator);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(_loggerProvider);
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        public static Task WaitForPredicate(Func<bool> condition, int timeout = 60 * 1000, int pollingInterval = 100, bool throwWhenDebugging = false)
        {
            return WaitForPredicate(() => Task.FromResult(condition()), pollingInterval, timeout, throwWhenDebugging);
        }

        public static async Task WaitForPredicate(Func<Task<bool>> condition, int timeout = 60 * 1000, int pollingInterval = 2 * 1000, bool throwWhenDebugging = false)
        {
            DateTime start = DateTime.Now;
            while (!await condition())
            {
                await Task.Delay(pollingInterval);
                bool shouldThrow = !Debugger.IsAttached || (Debugger.IsAttached && throwWhenDebugging);
                if (shouldThrow && (DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    throw new ApplicationException("Condition not reached within timeout.");
                }
            }
        }

        public class TestFunctions
        {
            public static void Trigger(
                [CosmosDBMongoTrigger(DatabaseName, CollectionName, ConnectionStringSetting = "CosmosDBMongoReal")] ChangeStreamDocument<BsonDocument> doc,
                ILogger logger)
            {
                logger.LogInformation(DateTime.Now.ToString());
                logger.LogInformation("Doc triggered");
                logger.LogInformation(doc.FullDocument.ToString());
            }
        }
    }
}
