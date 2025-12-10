// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Represents a lease document stored in the lease collection for change stream events.
    /// </summary>
    public class LeaseDocument
    {
        /// <summary>
        /// Unique identifier for the lease document.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Timestamp of the event for ordering.
        /// </summary>
        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Monitor level indicating Cluster/Database/Collection level monitoring.
        /// </summary>
        [BsonElement("monitorLevel")]
        [BsonRepresentation(BsonType.String)]
        public MonitorLevel MonitorLevel { get; set; }

        /// <summary>
        /// The monitored cluster identifier.
        /// </summary>
        [BsonElement("sourceCluster")]
        public string SourceCluster { get; set; }

        /// <summary>
        /// The source database name (nullable for cluster-level triggers).
        /// </summary>
        [BsonElement("sourceDatabase")]
        public string SourceDatabase { get; set; }

        /// <summary>
        /// The source collection name (nullable for database/cluster-level triggers).
        /// </summary>
        [BsonElement("sourceCollection")]
        public string SourceCollection { get; set; }

        /// <summary>
        /// The function identifier.
        /// </summary>
        [BsonElement("functionId")]
        public string FunctionId { get; set; }

        /// <summary>
        /// The change stream resume token for recovery.
        /// </summary>
        [BsonElement("resumeToken")]
        public BsonDocument ResumeToken { get; set; }

        /// <summary>
        /// The serialized change stream document.
        /// </summary>
        [BsonElement("changeEvent")]
        public BsonDocument ChangeEvent { get; set; }

        /// <summary>
        /// When the document was inserted.
        /// </summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }
}
