// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly MongoCollectionReference _reference;
        private readonly ILogger _logger;
        private IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;
        private MonitorLevel _triggerLevel;
        private IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _cursor;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private ActionBlock<ChangeStreamDocument<BsonDocument>> _workerPool;
        private const int MaxConcurrency = 32; // Maximum number of concurrent workers
        private CosmosDBMongoTriggerMetrics _currentMetrics;
        private readonly object _metricsLock = new object();
        private BsonDocument _resumeToken;
        private readonly object _resumeTokenLock = new object();
        private readonly object _cursorLock = new object();
        private const int MaxRetryDelaySeconds = 300;
        private const int InitialRetryDelaySeconds = 1;

        public CosmosDBMongoTriggerListener(ITriggeredFunctionExecutor executor, MongoCollectionReference reference, ILogger logger)
        {
            this._executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this._reference = reference;
            this._logger = logger;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._currentMetrics = new CosmosDBMongoTriggerMetrics();

            // Initialize the worker pool
            var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                CancellationToken = this._cancellationTokenSource.Token
            };

            this._workerPool = new ActionBlock<ChangeStreamDocument<BsonDocument>>(
                async document => await ProcessChangeAsync(document),
                executionDataflowBlockOptions);

            if (string.IsNullOrEmpty(this._reference.databaseName))
            {
                this._triggerLevel = MonitorLevel.Cluster;
            }
            else if (string.IsNullOrEmpty(this._reference.collectionName))
            {
                this._triggerLevel = MonitorLevel.Database;
            }
            else
            {
                this._triggerLevel = MonitorLevel.Collection;
            }
        }

        public void Cancel()
        {
            this.StopAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                    _cursor?.Dispose();
                    _workerPool?.Complete();
                }
                _disposed = true;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                this._cursor = await CreateChangeStreamAsync(null, cancellationToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ConsumeChangeStreamAsync();
                    }
                    catch (OperationCanceledException) when (this._cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Expected during shutdown
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(Events.OnError, $"Unexpected error in change stream consumer: {ex.Message}");
                    }
                }, this._cancellationTokenSource.Token);
                this._logger.LogDebug(Events.OnListenerStarted, "MongoDB trigger listener started.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(Events.OnListenerStartError, $"Starting the listener failed. Exception: {ex.Message}");
                throw;
            }
        }

        private PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> CreatePipeline()
        {
            var matchStage = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                .Match(change =>
                    change.OperationType == ChangeStreamOperationType.Insert ||
                    change.OperationType == ChangeStreamOperationType.Update ||
                    change.OperationType == ChangeStreamOperationType.Replace);
            
            return matchStage;
        }

        private async Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> CreateChangeStreamAsync(BsonDocument resumeAfter, CancellationToken cancellationToken)
        {
            var pipeline = CreatePipeline();

            var changeStreamOption = new ChangeStreamOptions
            {
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            };

            if (resumeAfter != null)
            {
                changeStreamOption.ResumeAfter = resumeAfter;
            }

            switch (this._triggerLevel)
            {
                case MonitorLevel.Cluster:
                    return await this._reference.client.WatchAsync(
                        pipeline, changeStreamOption, cancellationToken);
                case MonitorLevel.Database:
                    this._database = this._reference.client.GetDatabase(this._reference.databaseName);
                    return await this._database.WatchAsync(
                        pipeline, changeStreamOption, cancellationToken);
                case MonitorLevel.Collection:
                    this._database = this._reference.client.GetDatabase(this._reference.databaseName);
                    this._collection = this._database.GetCollection<BsonDocument>(this._reference.collectionName);
                    return await this._collection.WatchAsync(
                        pipeline, changeStreamOption, cancellationToken);
                default:
                    throw new InvalidOperationException("Unknown trigger level.");
            }
        }

        private async Task ConsumeChangeStreamAsync()
        {
            int retryDelaySeconds = InitialRetryDelaySeconds;

            while (!this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    bool hasMore;
                    lock (_cursorLock)
                    {
                        hasMore = _cursor != null;
                    }

                    while (hasMore && await this._cursor.MoveNextAsync(this._cancellationTokenSource.Token))
                    {
                        var batch = this._cursor.Current;
                        foreach (var change in batch)
                        {
                            await _workerPool.SendAsync(change, this._cancellationTokenSource.Token);

                            lock (_resumeTokenLock)
                            {
                                _resumeToken = change.ResumeToken;
                            }
                        }

                        retryDelaySeconds = InitialRetryDelaySeconds;
                    }
                }
                catch (Exception ex) when (
                    ex is MongoException || 
                    (ex is OperationCanceledException && !this._cancellationTokenSource.Token.IsCancellationRequested))
                {
                    this._logger.LogWarning(Events.OnChangeStreamError, 
                        $"Change stream cursor error: {ex.GetType().Name} - {ex.Message}. Will retry in {retryDelaySeconds} seconds.");

                    // Wait with exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), this._cancellationTokenSource.Token);

                    // Exponential backoff with max limit
                    retryDelaySeconds = Math.Min(retryDelaySeconds * 2, MaxRetryDelaySeconds);

                    // Attempt to reconnect with resume token
                    BsonDocument resumeToken;
                    lock (_resumeTokenLock)
                    {
                        resumeToken = _resumeToken;
                    }

                    try
                    {
                        this._logger.LogInformation(Events.OnChangeStreamReconnecting, 
                            resumeToken != null 
                                ? "Reconnecting to change stream with resume token..." 
                                : "Reconnecting to change stream from beginning...");

                        var newCursor = await CreateChangeStreamAsync(resumeToken, this._cancellationTokenSource.Token);
                        lock (_cursorLock)
                        {
                            this._cursor?.Dispose();
                            this._cursor = newCursor;
                        }

                        this._logger.LogInformation(Events.OnChangeStreamReconnected, 
                            "Successfully reconnected to change stream.");

                        retryDelaySeconds = InitialRetryDelaySeconds;
                    }
                    catch (OperationCanceledException) when (this._cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception reconnectEx)
                    {
                        this._logger.LogError(Events.OnChangeStreamError, 
                            $"Failed to reconnect to change stream: {reconnectEx.Message}");
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this._cancellationTokenSource.Cancel();

                _workerPool.Complete();
                await _workerPool.Completion;

                lock (_cursorLock)
                {
                    this._cursor?.Dispose();
                    this._cursor = null;
                }

                this._logger.LogDebug(Events.OnListenerStopped, "MongoDB trigger listener stopped.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(Events.OnListenerStopError, $"Stopping the listener failed. Exception: {ex.Message}");
            }
        }

        private async Task ProcessChangeAsync(ChangeStreamDocument<BsonDocument> change)
        {
            try
            {
                lock (_metricsLock)
                {
                    _currentMetrics.PendingEventsCount++;
                    CosmosDBMongoMetricsStore.AddMetrics(_reference.functionId, _reference.databaseName, _reference.collectionName, _currentMetrics);
                }

                try
                {
                    var triggerData = new TriggeredFunctionData
                    {
                        TriggerValue = change
                    };
                    var result = await this._executor.TryExecuteAsync(triggerData, this._cancellationTokenSource.Token);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning($"Function execution failed for document {change.DocumentKey}: {result.Exception}");
                    }
                }
                finally
                {
                    lock (_metricsLock)
                    {
                        _currentMetrics.PendingEventsCount--;
                        CosmosDBMongoMetricsStore.AddMetrics(_reference.functionId, _reference.databaseName, _reference.collectionName, _currentMetrics);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Events.OnError, $"Error processing change. Exception: {ex.Message}");
                throw;
            }
        }
    }
}