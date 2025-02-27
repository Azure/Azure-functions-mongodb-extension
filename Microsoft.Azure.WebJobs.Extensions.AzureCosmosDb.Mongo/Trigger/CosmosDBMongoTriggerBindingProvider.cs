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
                return Task.FromResult<ITriggerBinding>(null);
            attribute.ConnectionStringSetting = ResolveAttributeValue(attribute.ConnectionStringSetting);
            attribute.CollectionName = ResolveAttributeValue(attribute.CollectionName);
            attribute.DatabaseName = ResolveAttributeValue(attribute.DatabaseName);

            CosmosDBMongoTriggerContext triggerContext = this._configProvider.CreateTriggerContext(attribute);
            return
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(triggerContext, this._logger));
        }

        private string ResolveAttributeValue(string value)
        {
            return this._nameResolver.Resolve(value) ?? value;
        }
    }
}