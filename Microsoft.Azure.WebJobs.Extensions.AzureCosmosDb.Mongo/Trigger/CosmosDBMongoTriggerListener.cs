using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly CosmosDBMongoTriggerContext _context;
        private readonly ILogger _logger;
        private IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _collection;
        private MonitorLevel _triggerLevel;
        private IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _cursor;
        private CancellationTokenSource _cancellationTokenSource;

        public CosmosDBMongoTriggerListener(ITriggeredFunctionExecutor executor, CosmosDBMongoTriggerContext context, ILogger logger)
        {
            this._executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger;
            this._cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            this.StopAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this._triggerLevel = this._context.ResolvedAttribute.TriggerLevel;

            try
            {
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                    .Match(change =>
                        change.OperationType == ChangeStreamOperationType.Insert ||
                        change.OperationType == ChangeStreamOperationType.Update ||
                        change.OperationType == ChangeStreamOperationType.Replace ||
                        change.OperationType == ChangeStreamOperationType.Delete);

                var changeStreamOption = new ChangeStreamOptions
                    {
                        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
                    };
                switch (this._triggerLevel)
                {
                    case MonitorLevel.Cluster:
                        this._cursor = await this._context.MongoClient.WatchAsync(
                            pipeline, changeStreamOption, cancellationToken);
                        break;
                    case MonitorLevel.Database:
                        this._database = this._context.MongoClient.GetDatabase(this._context.ResolvedAttribute.DatabaseName);
                        this._cursor = await this._database.WatchAsync(
                            pipeline, changeStreamOption, cancellationToken);
                        break;
                    case MonitorLevel.Collection:
                        this._database = this._context.MongoClient.GetDatabase(this._context.ResolvedAttribute.DatabaseName);
                        this._collection = this._database.GetCollection<BsonDocument>(this._context.ResolvedAttribute.CollectionName);
                        this._cursor = await this._collection.WatchAsync(
                            pipeline, changeStreamOption, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown trigger level.");
                }

                _ = Task.Run(async () =>
                {
                    while (!this._cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        while (await this._cursor.MoveNextAsync(this._cancellationTokenSource.Token))
                        {
                            var batch = this._cursor.Current;
                            await ProcessBatchAsync(batch);
                        }
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this._cursor.Dispose();
                this._logger.LogDebug(Events.OnListenerStopped, "MongoDB trigger listener stopped.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(Events.OnListenerStopError, $"Stopping the listener failed. Exception: {ex.Message}");
            }
        }

        private async Task ProcessBatchAsync(IEnumerable<ChangeStreamDocument<BsonDocument>> batch)
        {
            try
            {
                var metrics = new CosmosDBMongoTriggerMetrics(batch.Count());
                CosmosDBMongoMetricsStore.AddMetrics(_context.ResolvedAttribute.FunctionId, _context.ResolvedAttribute.DatabaseName, _context.ResolvedAttribute.CollectionName, metrics);

                var tasks = batch.Select(async change =>
                {
                    try
                    {
                        var triggerData = new TriggeredFunctionData
                        {
                            TriggerValue = change
                        };
                        await this._executor.TryExecuteAsync(triggerData, this._cancellationTokenSource.Token);
                    }
                    finally
                    {
                        metrics.DecrementPendingCount();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(Events.OnError, $"Error processing batch of changes. Exception: {ex.Message}");
                throw;
            }
        }
    }
}