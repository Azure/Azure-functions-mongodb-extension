// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    [TestClass]
    public class LeaseCollectionManagerTests
    {
        private Mock<IMongoClient> _mockClient;
        private Mock<IMongoDatabase> _mockDatabase;
        private Mock<IMongoCollection<LeaseDocument>> _mockCollection;
        private Mock<ILogger> _mockLogger;
        private LeaseCollectionManager _manager;

        [TestInitialize]
        public void Setup()
        {
            _mockClient = new Mock<IMongoClient>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<LeaseDocument>>();
            _mockLogger = new Mock<ILogger>();

            _mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), null))
                .Returns(_mockDatabase.Object);

            _mockDatabase.Setup(d => d.GetCollection<LeaseDocument>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);

            _manager = new LeaseCollectionManager(
                _mockClient.Object,
                "testDatabase",
                "testCollection",
                _mockLogger.Object);
        }

        [TestMethod]
        public async Task InitializeAsync_CreatesIndexes()
        {
            // Arrange
            var mockIndexManager = new Mock<IMongoIndexManager<LeaseDocument>>();
            _mockCollection.Setup(c => c.Indexes).Returns(mockIndexManager.Object);
            
            mockIndexManager.Setup(m => m.CreateManyAsync(
                It.IsAny<CreateIndexModel<LeaseDocument>[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "index1", "index2" });

            // Act
            await _manager.InitializeAsync();

            // Assert
            mockIndexManager.Verify(m => m.CreateManyAsync(
                It.Is<CreateIndexModel<LeaseDocument>[]>(models => models.Length == 2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task InsertLeaseDocumentAsync_InsertsDocument()
        {
            // Arrange
            await _manager.InitializeAsync();
            var leaseDocument = new LeaseDocument
            {
                FunctionId = "testFunction",
                Timestamp = DateTime.UtcNow,
                SourceDatabase = "testDb",
                SourceCollection = "testColl"
            };

            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<LeaseDocument>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _manager.InsertLeaseDocumentAsync(leaseDocument);

            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(
                It.Is<LeaseDocument>(d => d.FunctionId == "testFunction"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InsertLeaseDocumentAsync_ThrowsIfNotInitialized()
        {
            // Arrange
            var leaseDocument = new LeaseDocument();

            // Act
            await _manager.InsertLeaseDocumentAsync(leaseDocument);

            // Assert - Exception expected
        }

        [TestMethod]
        public async Task FindOneAndDeleteAsync_ReturnsAndDeletesDocument()
        {
            // Arrange
            await _manager.InitializeAsync();
            var expectedDocument = new LeaseDocument
            {
                FunctionId = "testFunction",
                Timestamp = DateTime.UtcNow
            };

            _mockCollection.Setup(c => c.FindOneAndDeleteAsync(
                It.IsAny<FilterDefinition<LeaseDocument>>(),
                It.IsAny<FindOneAndDeleteOptions<LeaseDocument>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDocument);

            // Act
            var result = await _manager.FindOneAndDeleteAsync("testFunction", "testDb", "testColl");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("testFunction", result.FunctionId);
            _mockCollection.Verify(c => c.FindOneAndDeleteAsync(
                It.IsAny<FilterDefinition<LeaseDocument>>(),
                It.IsAny<FindOneAndDeleteOptions<LeaseDocument>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CountPendingDocumentsAsync_ReturnsCount()
        {
            // Arrange
            await _manager.InitializeAsync();
            long expectedCount = 42;

            _mockCollection.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<LeaseDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _manager.CountPendingDocumentsAsync("testFunction", "testDb", "testColl");

            // Assert
            Assert.AreEqual(expectedCount, result);
            _mockCollection.Verify(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<LeaseDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
