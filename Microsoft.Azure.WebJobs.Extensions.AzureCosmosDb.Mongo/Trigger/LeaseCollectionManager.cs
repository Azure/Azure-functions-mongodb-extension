// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Manages the lease collection for storing and consuming change stream events.
    /// </summary>
    public class LeaseCollectionManager
    {
        private readonly IMongoClient _leaseClient;
        private readonly string _leaseDatabaseName;
        private readonly string _leaseCollectionName;
        private readonly ILogger _logger;
        private IMongoDatabase _leaseDatabase;
        private IMongoCollection<LeaseDocument> _leaseCollection;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);

        public LeaseCollectionManager(
            IMongoClient leaseClient,
            string leaseDatabaseName,
            string leaseCollectionName,
            ILogger logger)
        {
            _leaseClient = leaseClient ?? throw new ArgumentNullException(nameof(leaseClient));
            _leaseDatabaseName = leaseDatabaseName ?? throw new ArgumentNullException(nameof(leaseDatabaseName));
            _leaseCollectionName = leaseCollectionName ?? throw new ArgumentNullException(nameof(leaseCollectionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the lease collection with proper indexes.
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_initialized)
                {
                    return;
                }

                _leaseDatabase = _leaseClient.GetDatabase(_leaseDatabaseName);
                _leaseCollection = _leaseDatabase.GetCollection<LeaseDocument>(_leaseCollectionName);
                _initialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }

            try
            {
                // Create indexes for efficient querying
                var indexKeysDefinition1 = Builders<LeaseDocument>.IndexKeys
                    .Ascending(d => d.Timestamp)
                    .Ascending(d => d.FunctionId);
                
                var indexKeysDefinition2 = Builders<LeaseDocument>.IndexKeys
                    .Ascending(d => d.FunctionId)
                    .Ascending(d => d.SourceDatabase)
                    .Ascending(d => d.SourceCollection)
                    .Ascending(d => d.Timestamp);

                var indexModel1 = new CreateIndexModel<LeaseDocument>(
                    indexKeysDefinition1,
                    new CreateIndexOptions { Name = "timestamp_functionId" });

                var indexModel2 = new CreateIndexModel<LeaseDocument>(
                    indexKeysDefinition2,
                    new CreateIndexOptions { Name = "functionId_source_timestamp" });

                await _leaseCollection.Indexes.CreateManyAsync(
                    new[] { indexModel1, indexModel2 },
                    cancellationToken);

                _logger.LogInformation("Lease collection initialized with indexes.");
            }
            catch (Exception ex)
            {
                // Index creation failures are non-fatal - collection can still work
                _logger.LogWarning($"Failed to create indexes on lease collection: {ex.Message}");
            }
        }

        /// <summary>
        /// Inserts a lease document into the lease collection.
        /// </summary>
        public async Task InsertLeaseDocumentAsync(
            LeaseDocument leaseDocument,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("LeaseCollectionManager must be initialized before use.");
            }

            await _leaseCollection.InsertOneAsync(leaseDocument, null, cancellationToken);
        }

        /// <summary>
        /// Retrieves and deletes the oldest pending lease document for the specified function.
        /// </summary>
        public async Task<LeaseDocument> FindOneAndDeleteAsync(
            string functionId,
            string sourceDatabase,
            string sourceCollection,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("LeaseCollectionManager must be initialized before use.");
            }

            var filter = Builders<LeaseDocument>.Filter.And(
                Builders<LeaseDocument>.Filter.Eq(d => d.FunctionId, functionId),
                Builders<LeaseDocument>.Filter.Eq(d => d.SourceDatabase, sourceDatabase),
                Builders<LeaseDocument>.Filter.Eq(d => d.SourceCollection, sourceCollection)
            );

            var sort = Builders<LeaseDocument>.Sort.Ascending(d => d.Timestamp);

            var options = new FindOneAndDeleteOptions<LeaseDocument>
            {
                Sort = sort
            };

            return await _leaseCollection.FindOneAndDeleteAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Counts the number of pending lease documents for the specified function.
        /// </summary>
        public async Task<long> CountPendingDocumentsAsync(
            string functionId,
            string sourceDatabase,
            string sourceCollection,
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("LeaseCollectionManager must be initialized before use.");
            }

            var filter = Builders<LeaseDocument>.Filter.And(
                Builders<LeaseDocument>.Filter.Eq(d => d.FunctionId, functionId),
                Builders<LeaseDocument>.Filter.Eq(d => d.SourceDatabase, sourceDatabase),
                Builders<LeaseDocument>.Filter.Eq(d => d.SourceCollection, sourceCollection)
            );

            return await _leaseCollection.CountDocumentsAsync(filter, null, cancellationToken);
        }
    }
}
