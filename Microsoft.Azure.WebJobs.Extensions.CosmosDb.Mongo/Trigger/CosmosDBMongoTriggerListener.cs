using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoTriggerListener : IListener
    {
        private readonly ITriggeredFunctionExecutor executor;
        private readonly CosmosDBMongoTriggerContext context;
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<BsonDocument> collection;
        private IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor;
        private CancellationTokenSource cancellationTokenSource;

        public CosmosDBMongoTriggerListener(ITriggeredFunctionExecutor executor, CosmosDBMongoTriggerContext context)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.cancellationTokenSource = new CancellationTokenSource();
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
            this.database = this.context.MongoClient.GetDatabase(this.context.ResolvedAttribute.DatabaseName);
            this.collection = this.database.GetCollection<BsonDocument>(this.context.ResolvedAttribute.CollectionName);

            try
            {
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                    .Match(change =>
                        change.OperationType == ChangeStreamOperationType.Insert ||
                        change.OperationType == ChangeStreamOperationType.Update ||
                        change.OperationType == ChangeStreamOperationType.Delete);

                this.cursor = await this.collection.WatchAsync(
                    pipeline,
                    new ChangeStreamOptions
                    {
                        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
                    }, cancellationToken);

                _ = Task.Run(async () =>
                {
                    while (!this.cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        while (await this.cursor.MoveNextAsync(this.cancellationTokenSource.Token))
                        {
                            var batch = this.cursor.Current;
                            foreach (var change in batch)
                            {
                                var triggerData = new TriggeredFunctionData
                                {
                                    TriggerValue = change
                                };
                                await this.executor.TryExecuteAsync(triggerData, this.cancellationTokenSource.Token);
                            }
                        }
                    }
                }, this.cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.cursor.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task ListenForChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await this.cursor.MoveNextAsync())
                {
                    foreach (var change in this.cursor.Current)
                    {
                        var functionData = new TriggeredFunctionData
                        {
                            TriggerValue = change,
                        };
                        var task = await this.executor.TryExecuteAsync(functionData, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("MongoDB trigger listener cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}