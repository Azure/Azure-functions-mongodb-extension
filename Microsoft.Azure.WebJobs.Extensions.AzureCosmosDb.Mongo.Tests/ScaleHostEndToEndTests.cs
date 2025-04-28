using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    [TestClass]
    [TestCategory("EmulatorRequired")]
    public class ScaleHostEndToEndTests
    {
        private const string FunctionName = "ScalerE2ETestTrigger";
        private const string DatabaseName = "ScaleTestDatabase";
        private const string CollectionName = "ScaleTestCollection";
        private const string Connection = "CosmosDBMongoReal";
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        private string ConnectionString => Environment.GetEnvironmentVariable(Connection)!;

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
        [DataRow(false)]
        [DataRow(true)]
        public async Task ScaleHostEndToEndTest(bool tbsEnabled)
        {
            string triggers = $@"{{
            ""triggers"": [
                {{
                    ""name"": ""ScalerE2ETest"",
                    ""type"": ""CosmosDBMongoTrigger"",
                    ""direction"": ""in"",
                    ""connection"": ""{Connection}"",
                    ""databaseName"": ""{DatabaseName}"",
                    ""collectionName"": ""{CollectionName}"",
                    ""MaxItemsPerInvocation"": 1,
                    ""functionName"": ""{FunctionName}"",
                    ""MaxWorkPerInstance"": 4,
                    ""MinSampleCount"": 2,
                }}
             ]}}";

            string hostId = "test-host";

            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { Connection, ConnectionString }
            };

            IHostBuilder hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(settings);
                })
                .ConfigureLogging(configure =>
                {
                    configure.SetMinimumLevel(LogLevel.Debug);
                    configure.AddProvider(_loggerProvider);
                })
                .ConfigureServices(services =>
                {
                    services.AddAzureClientsCore();
                    services.AddAzureStorageScaleServices();
                    services.AddSingleton<INameResolver, FakeNameResolver>();
                })
                .ConfigureWebJobsScale((context, builder) =>
                {
                    builder.AddCosmosDBMongo();
                    builder.UseHostId(hostId);

                    foreach (var jtoken in JObject.Parse(triggers)["triggers"])
                    {
                        TriggerMetadata metadata = new TriggerMetadata(jtoken as JObject);
                        builder.AddCosmosDBMongoScaleForTrigger(metadata);
                    }
                },
                scaleOptions =>
                {
                    scaleOptions.IsTargetScalingEnabled = tbsEnabled;
                    scaleOptions.MetricsPurgeEnabled = false;
                    scaleOptions.ScaleMetricsMaxAge = TimeSpan.FromMinutes(4);
                    scaleOptions.IsRuntimeScalingEnabled = true;
                    scaleOptions.ScaleMetricsSampleInterval = TimeSpan.FromSeconds(1);
                });

            IHost triggerHost = await CreateTriggerHostAsync();

            IHost scaleHost = hostBuilder.Build();
            await scaleHost.StartAsync();

            var mongoClient = new MongoClient(ConnectionString);
            var database = mongoClient.GetDatabase(DatabaseName);
            var collection = database.GetCollection<BsonDocument>(CollectionName);

            _loggerProvider.ClearAllLogMessages();

            for (int j = 0; j < 5; j++)
            {
                var documents = new List<BsonDocument>();
                for (int i = 0; i < 300; i++)
                {
                    documents.Add(new BsonDocument()
                    {
                        {"timestamp", DateTime.UtcNow.ToString("o")},
                        {"testId", Guid.NewGuid().ToString()}
                    });
                }
                await collection.InsertManyAsync(documents);

                await Task.Delay(500);
            }

            await WaitForPredicate(async () =>
            {
                IScaleStatusProvider scaleManager = scaleHost.Services.GetService<IScaleStatusProvider>();

                if (scaleManager == null)
                {
                    return false;
                }

                var context = new ScaleStatusContext();
                var scaleStatus = await scaleManager.GetScaleStatusAsync(context);

                if (scaleStatus == null)
                {
                    return false;
                }

                bool scaledOut = false;
                if (!tbsEnabled)
                {
                    scaledOut = scaleStatus.Vote == ScaleVote.ScaleOut &&
                                scaleStatus.TargetWorkerCount == null &&
                                scaleStatus.FunctionScaleStatuses[FunctionName].Vote == ScaleVote.ScaleOut;

                    if (scaledOut)
                    {
                        var logMessages = _loggerProvider.GetAllLogMessages().Select(p => p.FormattedMessage).ToArray();
                        Assert.IsTrue(logMessages.Any(p => p != null && p.Contains("1 scale monitors to sample")));
                    }
                }
                else
                {
                    scaledOut = scaleStatus.Vote == ScaleVote.ScaleOut &&
                                scaleStatus.TargetWorkerCount > 1 && 
                                scaleStatus.FunctionTargetScalerResults[FunctionName].TargetWorkerCount > 1 &&
                                scaleStatus.FunctionScaleStatuses.Count == 0;

                    if (scaledOut)
                    {
                        var logMessages = _loggerProvider.GetAllLogMessages().Select(p => p.FormattedMessage).ToArray();
                        Assert.IsTrue(logMessages.Any(p => p != null && p.Contains("1 target scalers to sample")));
                    }
                }

                if (scaledOut && !tbsEnabled)
                {
                    var logMessages = _loggerProvider.GetAllLogMessages().Select(p => p.FormattedMessage).ToArray();
                    Assert.IsTrue(logMessages.Any(p => p != null && p.Contains("Scaling out based on votes")));
                }

                return scaledOut;
            }, pollingInterval: 200, timeout: 30000);

            var triggerLogs = _loggerProvider.GetAllLogMessages()
                .Where(m => m.FormattedMessage != null && m.FormattedMessage.Contains("Doc triggered"))
                .ToList();

            Assert.IsTrue(triggerLogs.Count > 0, "");

            await scaleHost.StopAsync();
        }

        private async Task<IHost> CreateTriggerHostAsync()
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
                    services.AddSingleton<INameResolver, FakeNameResolver>();
                    
                    services.AddSingleton<IConfiguration>(provider =>
                    {
                        var configBuilder = new ConfigurationBuilder();
                        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { Connection, ConnectionString }
                        });
                        return configBuilder.Build();
                    });
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
            public static async Task ScalerE2ETestTrigger(
                [CosmosDBMongoTrigger(DatabaseName, CollectionName, ConnectionStringSetting = "CosmosDBMongoReal")] ChangeStreamDocument<BsonDocument> doc,
                ILogger logger)
            {
                logger.LogInformation("Doc triggered");
                await Task.Delay(5000); // Simulate some work to make scaling more likely
            }
        }

        public class FakeNameResolver : INameResolver
        {
            public string Resolve(string name) => name;
        }
    }
}