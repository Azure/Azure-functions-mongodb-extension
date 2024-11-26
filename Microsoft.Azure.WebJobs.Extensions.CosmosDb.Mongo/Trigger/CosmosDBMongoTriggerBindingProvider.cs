using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly CosmosDBMongoConfigProvider configProvider;
        private readonly INameResolver nameResolver;

        public CosmosDBMongoTriggerBindingProvider(INameResolver nameResolver, CosmosDBMongoConfigProvider configProvider)
        {
            this.nameResolver = nameResolver;
            this.configProvider = configProvider;
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

            CosmosDBMongoTriggerContext triggerContext = this.configProvider.CreateContextTrigger(attribute);
            return
                Task.FromResult<ITriggerBinding>(new CosmosDBMongoTriggerBinding(triggerContext));
        }

        private string ResolveAttributeValue(string value)
        {
            return this.nameResolver.Resolve(value) ?? value;
        }
    }
}