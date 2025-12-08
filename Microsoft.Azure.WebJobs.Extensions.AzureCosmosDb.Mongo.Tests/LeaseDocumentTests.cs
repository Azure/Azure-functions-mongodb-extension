// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    [TestClass]
    public class LeaseDocumentTests
    {
        [TestMethod]
        public void LeaseDocument_SerializesToBson()
        {
            // Arrange
            var leaseDocument = new LeaseDocument
            {
                Timestamp = DateTime.UtcNow,
                MonitorLevel = MonitorLevel.Collection,
                SourceCluster = "cluster1",
                SourceDatabase = "db1",
                SourceCollection = "coll1",
                FunctionId = "func1",
                ResumeToken = new BsonDocument("_data", "token123"),
                ChangeEvent = new BsonDocument("operationType", "insert"),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var bsonDocument = leaseDocument.ToBsonDocument();

            // Assert
            Assert.IsNotNull(bsonDocument);
            Assert.IsTrue(bsonDocument.Contains("timestamp"));
            Assert.IsTrue(bsonDocument.Contains("monitorLevel"));
            Assert.IsTrue(bsonDocument.Contains("sourceCluster"));
            Assert.IsTrue(bsonDocument.Contains("sourceDatabase"));
            Assert.IsTrue(bsonDocument.Contains("sourceCollection"));
            Assert.IsTrue(bsonDocument.Contains("functionId"));
            Assert.IsTrue(bsonDocument.Contains("resumeToken"));
            Assert.IsTrue(bsonDocument.Contains("changeEvent"));
            Assert.IsTrue(bsonDocument.Contains("createdAt"));
        }

        [TestMethod]
        public void LeaseDocument_DeserializesFromBson()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var bsonDocument = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId().ToString() },
                { "timestamp", now },
                { "monitorLevel", "Collection" },
                { "sourceCluster", "cluster1" },
                { "sourceDatabase", "db1" },
                { "sourceCollection", "coll1" },
                { "functionId", "func1" },
                { "resumeToken", new BsonDocument("_data", "token123") },
                { "changeEvent", new BsonDocument("operationType", "insert") },
                { "createdAt", now }
            };

            // Act
            var leaseDocument = BsonSerializer.Deserialize<LeaseDocument>(bsonDocument);

            // Assert
            Assert.IsNotNull(leaseDocument);
            Assert.AreEqual(MonitorLevel.Collection, leaseDocument.MonitorLevel);
            Assert.AreEqual("cluster1", leaseDocument.SourceCluster);
            Assert.AreEqual("db1", leaseDocument.SourceDatabase);
            Assert.AreEqual("coll1", leaseDocument.SourceCollection);
            Assert.AreEqual("func1", leaseDocument.FunctionId);
            Assert.IsNotNull(leaseDocument.ResumeToken);
            Assert.IsNotNull(leaseDocument.ChangeEvent);
        }

        [TestMethod]
        public void LeaseDocument_SupportsNullableFields()
        {
            // Arrange
            var leaseDocument = new LeaseDocument
            {
                Timestamp = DateTime.UtcNow,
                MonitorLevel = MonitorLevel.Cluster,
                SourceCluster = "cluster1",
                SourceDatabase = null, // Nullable for cluster-level
                SourceCollection = null, // Nullable for cluster-level
                FunctionId = "func1",
                ResumeToken = new BsonDocument(),
                ChangeEvent = new BsonDocument(),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var bsonDocument = leaseDocument.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<LeaseDocument>(bsonDocument);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(MonitorLevel.Cluster, deserialized.MonitorLevel);
            Assert.IsNull(deserialized.SourceDatabase);
            Assert.IsNull(deserialized.SourceCollection);
        }
    }
}
