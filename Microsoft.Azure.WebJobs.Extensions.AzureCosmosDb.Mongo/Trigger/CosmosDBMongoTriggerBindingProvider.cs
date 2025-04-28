using Amazon.Util.Internal;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;
        private readonly INameResolver _nameResolver;
        private readonly ILogger _logger;

        public CosmosDBMongoTriggerBindingProvider(INameResolver nameResolver, CosmosDBMongoConfigProvider configProvider, ILoggerFactory loggerFactory)
        {
            this._nameResolver = nameResolver;
            this._configProvider = configProvider;
            this._logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory(CosmosDBMongoConstant.AzureFunctionTelemetryCategory));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            
            var attribute = context.Parameter.GetCustomAttribute<CosmosDBMongoTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            string connectionString = _configProvider.ResolveConnectionString(attribute.ConnectionStringSetting);
            string functionId = context.Parameter.Member.Name;
            var reference = new MongoCollectionReference(
                        _configProvider.GetService(connectionString),
                        ResolveAttributeValue(attribute.DatabaseName),
                        ResolveAttributeValue(attribute.CollectionName));
            reference.functionId = functionId;
            return
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(context.Parameter,
                    reference,
                    this._logger));
        }

        private string ResolveAttributeValue(string value)
        {
            return this._nameResolver.Resolve(value) ?? value;
        }
    }
}