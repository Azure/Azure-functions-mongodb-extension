using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    [TestClass]
    public class CosmosDBMongoMockEndToEndTests
    {
        private const string DatabaseName = "TestDatabase";
        private const string CollectionName = "TestCollection";

        private const string DefaultConnStr = "AccountEndpoint=https://default;AccountKey=ZGVmYXVsdA==;";

        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        public CosmosDBMongoMockEndToEndTests()
        {
            _loggerFactory.AddProvider(_loggerProvider);
        }

        private Mock<IMongoCollection<T>> CreateMockCollection<T>()
        {
            var mock = new Mock<IMongoCollection<T>>(MockBehavior.Strict);
            mock
                .Setup(m => m.InsertOneAsync(It.IsAny<T>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private (Mock<ICosmosDBMongoBindingCollectorFactory>, IEnumerable<dynamic>) CreateMocks()
        {
            var monitoredDatabaseMock = new Mock<IMongoDatabase>(MockBehavior.Strict);

            var bsonMock = CreateMockCollection<BsonDocument>();
            monitoredDatabaseMock
                .Setup(m => m.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns(bsonMock.Object);

            var itemMock = CreateMockCollection<Item>();
            monitoredDatabaseMock
                .Setup(m => m.GetCollection<Item>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns(itemMock.Object);

            var serviceMock = new Mock<IMongoClient>(MockBehavior.Strict);
            serviceMock.Setup(m => m.GetDatabase(It.IsAny<string>(), default)).Returns(monitoredDatabaseMock.Object);

            var factoryMock = new Mock<ICosmosDBMongoBindingCollectorFactory>(MockBehavior.Strict);
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(serviceMock.Object);

            return (factoryMock, new List<dynamic> { monitoredDatabaseMock, bsonMock, itemMock, serviceMock }.AsEnumerable());
        }

        [TestInitialize]
        public void Setup()
        {
            Environment.SetEnvironmentVariable(CosmosDBMongoConstant.DefaultConnectionStringKey, DefaultConnStr);
        }

        [TestMethod]
        public async Task TestClient()
        {
            var (factoryMock, mocks) = CreateMocks();
            await RunTestAsync("Client", factoryMock.Object);
        }

        [TestMethod]
        public async Task TestCollector()
        {
            var (factoryMock, mocks) = CreateMocks();
            await RunTestAsync("Collector", factoryMock.Object);
        }

        [TestMethod]
        public async Task TestOutputs()
        {
            var (factoryMock, mocks) = CreateMocks();
            await RunTestAsync("Outputs", factoryMock.Object);
        }

        [TestMethod]
        public async Task TestInputs()
        {
            var (factoryMock, mocks) = CreateMocks();
            await RunTestAsync("Inputs", factoryMock.Object);
        }

        private Task RunTestAsync(string testName, ICosmosDBMongoBindingCollectorFactory factory, object argument = null)
        {
            return RunTestAsync(typeof(CosmosDBMongoEndToEndFunctions), testName, factory, argument);
        }

        private async Task RunTestAsync(Type testType, string testName, ICosmosDBMongoBindingCollectorFactory factory, object argument = null, bool includeDefaultConnectionString = true)
        {
            ExplicitTypeLocator typeLocator = new ExplicitTypeLocator(testType);

            IHost host = new HostBuilder()
                 .ConfigureWebJobs(builder =>
                 {
                     builder.AddCosmosDBMongo();
                 })
                 .ConfigureServices(services =>
                 {
                     services.AddSingleton<ICosmosDBMongoBindingCollectorFactory>(factory);
                     services.AddSingleton<ITypeLocator>(typeLocator);
                 })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                 })
             .Build();

            await host.StartAsync();
            await ((JobHost)host.Services.GetService<IJobHost>()).CallAsync(typeof(CosmosDBMongoEndToEndFunctions).GetMethod(testName), null);
            await host.StopAsync();
        }


        private class CosmosDBMongoEndToEndFunctions
        {
            [NoAutomaticTrigger()]
            public static void Client(
                [CosmosDBMongo] IMongoClient client)
            {
                Assert.IsNotNull(client);
            }

            [NoAutomaticTrigger()]
            public async Task Collector(
                [CosmosDBMongo(DatabaseName, CollectionName)] IAsyncCollector<BsonDocument> collector,
                [CosmosDBMongo(DatabaseName, CollectionName)] IAsyncCollector<Item> itemCollector,
                [CosmosDBMongo(DatabaseName, CollectionName)] ICollector<BsonDocument> syncCollector,
                [CosmosDBMongo(DatabaseName, CollectionName)] ICollector<Item> syncItemCollector)
            {
                for (int i = 0; i < 3; i++)
                {
                    await collector.AddAsync(new BsonDocument());
                    await itemCollector.AddAsync(new Item());
                    syncCollector.Add(new BsonDocument());
                    syncItemCollector.Add(new Item());
                }
            }

            [NoAutomaticTrigger()]
            public void Outputs(
                [CosmosDBMongo(DatabaseName, CollectionName)] out BsonDocument doc,
                [CosmosDBMongo(DatabaseName, CollectionName)] out BsonDocument[] docs,
                [CosmosDBMongo(DatabaseName, CollectionName)] out Item obj,
                [CosmosDBMongo(DatabaseName, CollectionName)] out Item[] items)
            {
                doc = new BsonDocument();
                docs = new BsonDocument[1] { new BsonDocument() };

                obj = new Item();
                items = new Item[1] { new Item() };
            }

            [NoAutomaticTrigger()]
            public void Inputs(
                [CosmosDBMongo(DatabaseName)] IMongoDatabase db,
                [CosmosDBMongo(DatabaseName, CollectionName)] IMongoCollection<BsonDocument> coll)
            {
                Assert.IsNotNull(db);
                Assert.IsNotNull(coll);
            }
        }
    }
}
