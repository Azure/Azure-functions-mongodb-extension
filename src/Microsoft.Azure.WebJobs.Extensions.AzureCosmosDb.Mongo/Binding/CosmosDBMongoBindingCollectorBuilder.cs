using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoBindingCollectorBuilder<T> : IConverter<CosmosDBMongoAttribute, IAsyncCollector<T>>
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;
        private readonly ILogger _logger;

        public CosmosDBMongoBindingCollectorBuilder(CosmosDBMongoConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            this._configProvider = configProvider;
            this._logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory(CosmosDBMongoConstant.AzureFunctionTelemetryCategory));
        }

        public IAsyncCollector<T> Convert(CosmosDBMongoAttribute attribute)
        {
            return new CosmosDBMongoBindingAsyncCollector<T>(attribute, this._configProvider.ResolveCollectionReference(attribute), this._logger);
        }
    }
}