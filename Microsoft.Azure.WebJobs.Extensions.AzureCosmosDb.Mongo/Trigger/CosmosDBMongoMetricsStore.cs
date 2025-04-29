// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal static class CosmosDBMongoMetricsStore
    {
        private static readonly ConcurrentDictionary<string, Queue<CosmosDBMongoTriggerMetrics>> _metricsHistory 
            = new ConcurrentDictionary<string, Queue<CosmosDBMongoTriggerMetrics>>();
        private static readonly ConcurrentDictionary<string, CosmosDBMongoTriggerMetrics> _currentMetrics
            = new ConcurrentDictionary<string, CosmosDBMongoTriggerMetrics>();
        private static readonly CancellationTokenSource _cleanupTokenSource = new CancellationTokenSource();
        
        private const int MaxSampleCount = 100;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MetricsSnapshotInterval = TimeSpan.FromSeconds(5);

        static CosmosDBMongoMetricsStore()
        {
            StartCleanupTask();
            StartMetricsSnapshotTask();
        }

        public static CosmosDBMongoTriggerMetrics GetMetrics(string functionId, string databaseName, string collectionName)
        {
            string key = $"{functionId}-{databaseName}-{collectionName}";
            return _currentMetrics.GetOrAdd(key, _ => new CosmosDBMongoTriggerMetrics());
        }

        public static void AddMetrics(string functionId, string databaseName, string collectionName, CosmosDBMongoTriggerMetrics metrics)
        {
            string key = $"{functionId}-{databaseName}-{collectionName}";
            _currentMetrics.AddOrUpdate(key, metrics, (_, existing) =>
            {
                // We don't need to update the existing metrics since it's the same instance
                // being updated by the listener
                return existing;
            });
        }
        
        public static CosmosDBMongoTriggerMetrics[] GetMetricsHistory(string functionId, string databaseName, string collectionName)
        {
            string key = $"{functionId}-{databaseName}-{collectionName}";
            if (_metricsHistory.TryGetValue(key, out var queue))
            {
                lock (queue)
                {
                    return queue.ToArray();
                }
            }
            return Array.Empty<CosmosDBMongoTriggerMetrics>();
        }

        private static void StartCleanupTask()
        {
            Task.Run(async () =>
            {
                while (!_cleanupTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(CleanupInterval, _cleanupTokenSource.Token);
                    CleanupOldMetrics();
                }
            }, _cleanupTokenSource.Token);
        }

        private static void StartMetricsSnapshotTask()
        {
            Task.Run(async () =>
            {
                while (!_cleanupTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(MetricsSnapshotInterval, _cleanupTokenSource.Token);
                    TakeMetricsSnapshot();
                }
            }, _cleanupTokenSource.Token);
        }

        private static void TakeMetricsSnapshot()
        {
            foreach (var kvp in _currentMetrics)
            {
                var queue = _metricsHistory.GetOrAdd(kvp.Key, _ => new Queue<CosmosDBMongoTriggerMetrics>());
                var snapshot = new CosmosDBMongoTriggerMetrics
                {
                    PendingEventsCount = kvp.Value.PendingEventsCount,
                    Timestamp = DateTime.UtcNow
                };

                lock (queue)
                {
                    queue.Enqueue(snapshot);
                    while (queue.Count > MaxSampleCount)
                    {
                        queue.Dequeue();
                    }
                }
            }
        }

        private static void CleanupOldMetrics()
        {
            var keysToRemove = new List<string>();

            foreach (var key in _metricsHistory.Keys)
            {
                if (_metricsHistory.TryGetValue(key, out var queue))
                {
                    lock (queue)
                    {
                        if (queue.Count == 0 || (DateTime.UtcNow - queue.Last().Timestamp) > TimeSpan.FromHours(1))
                        {
                            keysToRemove.Add(key);
                        }
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                _metricsHistory.TryRemove(key, out _);
                _currentMetrics.TryRemove(key, out _);
            }
        }
    }
}