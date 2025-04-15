using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    internal class CosmosDBMongoTestUtility
    {
        public const string DatabaseName = "TestDB";
        public const string CollectionName = "TestCollection";

        public static Mock<IMongoCollection<T>> SetupCollectionMock<T>(Mock<IMongoDatabase> mockDatabase)
        {
            var mockCollection = new Mock<IMongoCollection<T>>();
            mockDatabase
                .Setup(db => db.GetCollection<T>(It.Is<string>(name => name == CollectionName), null))
                .Returns(mockCollection.Object);

            return mockCollection;
        }

        public static Mock<IMongoDatabase> SetupDatabaseMock(Mock<MongoClient> mockClient)
        {
            var mockDatabase = new Mock<IMongoDatabase>();

            mockClient
                .Setup(client => client.GetDatabase(It.Is<string>(name => name == DatabaseName), null))
                .Returns(mockDatabase.Object);

            return mockDatabase;
        }

        public static IConfiguration BuildConfiguration(List<Tuple<string, string>> configs)
        {
            var mock = new Mock<IConfiguration>();
            foreach (var config in configs)
            {
                var section = new Mock<IConfigurationSection>();
                section.Setup(s => s.Value).Returns(config.Item2);
                mock.Setup(c => c.GetSection(It.Is<string>(sectionName => sectionName == config.Item1)))
                    .Returns(section.Object);
            }

            return mock.Object;
        }

    }
}
