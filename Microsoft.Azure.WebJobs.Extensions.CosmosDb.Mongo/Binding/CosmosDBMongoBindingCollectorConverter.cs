using Microsoft.Azure.WebJobs;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    internal class CosmosDBMongoBindingCollectorConverter<T> : IConverter<CosmosDBMongoAttribute, IAsyncCollector<T>>
    {
        private readonly CosmosDBMongoConfigProvider configProvider;

        public CosmosDBMongoBindingCollectorConverter(CosmosDBMongoConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public IAsyncCollector<T> Convert(CosmosDBMongoAttribute attribute)
        {
            CosmosDBMongoContext context = this.configProvider.CreateContext(attribute);
            return new CosmosDBMongoBindingAsyncCollector<T>(context);
        }
    }
}