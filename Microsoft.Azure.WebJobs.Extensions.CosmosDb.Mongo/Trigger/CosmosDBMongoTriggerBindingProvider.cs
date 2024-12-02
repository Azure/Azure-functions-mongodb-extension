using Amazon.Util.Internal;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;
        private readonly INameResolver _nameResolver;

        public CosmosDBMongoTriggerBindingProvider(INameResolver nameResolver, CosmosDBMongoConfigProvider configProvider)
        {
            this._nameResolver = nameResolver;
            this._configProvider = configProvider;
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
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(triggerContext));
        }

        private string ResolveAttributeValue(string value)
        {
            return this._nameResolver.Resolve(value) ?? value;
        }
    }
}