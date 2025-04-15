// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
// {
//     internal static class CosmosDBMongoMetricsStore
//     {
//         private static readonly ConcurrentDictionary<string, Queue<CosmosDBMongoTriggerMetrics>> _metricsHistory 
//             = new ConcurrentDictionary<string, Queue<CosmosDBMongoTriggerMetrics>>();
//         private static readonly CancellationTokenSource _cleanupTokenSource = new CancellationTokenSource();
        
//         private const int MaxSampleCount = 30;
//         private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);

//         static CosmosDBMongoMetricsStore()
//         {
//             StartCleanupTask();
//         }

//         public static CosmosDBMongoTriggerMetrics GetMetrics(string functionId, string databaseName, string collectionName)
//         {
//             string key = $"{functionId}-{databaseName}-{collectionName}";
//             var queue = _metricsHistory.GetOrAdd(key, _ => new Queue<CosmosDBMongoTriggerMetrics>());
            
//             lock (queue)
//             {
//                 return queue.Count > 0 ? queue.Last() : new CosmosDBMongoTriggerMetrics();
//             }
//         }

//         public static void AddMetrics(string functionId, string databaseName, string collectionName, CosmosDBMongoTriggerMetrics metrics)
//         {
//             string key = $"{functionId}-{databaseName}-{collectionName}";
//             var queue = _metricsHistory.GetOrAdd(key, _ => new Queue<CosmosDBMongoTriggerMetrics>());
            
//             lock (queue)
//             {
//                 queue.Enqueue(metrics);
//             }
//         }
        
//         public static CosmosDBMongoTriggerMetrics[] GetMetricsHistory(string functionId, string databaseName, string collectionName)
//         {
//             string key = $"{functionId}-{databaseName}-{collectionName}";
//             if (_metricsHistory.TryGetValue(key, out var queue))
//             {
//                 lock (queue)
//                 {
//                     return queue.ToArray();
//                 }
//             }
//             return Array.Empty<CosmosDBMongoTriggerMetrics>();
//         }

//         private static void StartCleanupTask()
//         {
//             Task.Run(async () =>
//             {
//                 while (!_cleanupTokenSource.Token.IsCancellationRequested)
//                 {
//                     await Task.Delay(CleanupInterval, _cleanupTokenSource.Token);
//                     CleanupOldMetrics();
//                 }
//             }, _cleanupTokenSource.Token);
//         }

//         private static void CleanupOldMetrics()
//         {
//             foreach (var key in _metricsHistory.Keys)
//             {
//                 if (_metricsHistory.TryGetValue(key, out var queue))
//                 {
//                     lock (queue)
//                     {
//                         while (queue.Count > MaxSampleCount)
//                         {
//                             queue.Dequeue();
//                         }
//                     }
//                 }
//             }
//         }
//     }
// }