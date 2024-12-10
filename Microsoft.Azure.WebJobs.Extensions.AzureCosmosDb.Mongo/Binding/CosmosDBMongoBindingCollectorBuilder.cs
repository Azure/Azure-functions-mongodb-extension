namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoBindingCollectorBuilder<T> : IConverter<CosmosDBMongoAttribute, IAsyncCollector<T>>
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;

        public CosmosDBMongoBindingCollectorBuilder(CosmosDBMongoConfigProvider configProvider)
        {
            this._configProvider = configProvider;
        }

        public IAsyncCollector<T> Convert(CosmosDBMongoAttribute attribute)
        {
            CosmosDBMongoContext context = this._configProvider.CreateContext(attribute);
            return new CosmosDBMongoBindingAsyncCollector<T>(context);
        }
    }
}