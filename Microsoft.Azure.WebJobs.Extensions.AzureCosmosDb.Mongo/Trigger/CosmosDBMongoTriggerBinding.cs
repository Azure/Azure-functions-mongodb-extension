using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoTriggerBinding : ITriggerBinding
    {
        private readonly CosmosDBMongoTriggerContext _triggerContext;
        private readonly ILogger _logger;

        public CosmosDBMongoTriggerBinding(CosmosDBMongoTriggerContext context, ILogger logger)
        {
            this._triggerContext = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger;
        }

        public Type TriggerValueType => typeof(ChangeStreamDocument<BsonDocument>);

        public IReadOnlyDictionary<string, Type> BindingDataContract => new Dictionary<string, Type>
        {
            { "CosmosDBMongoTrigger", typeof(ChangeStreamDocument<BsonDocument>) }
        };

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var valueProvider = new CosmosDBMongoValueProvider(value);
            var bindingData = new Dictionary<string, object>
            {
                { "CosmosDBMongoTrigger", value }
            };
            return new TriggerData(valueProvider, bindingData);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Task.FromResult<IListener>(
                new CosmosDBMongoTriggerListener(context.Executor, this._triggerContext, this._logger));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new TriggerParameterDescriptor()
            {
                Name = "CosmosDBMongoTrigger",
            };
        }

        private class CosmosDBMongoValueProvider : IValueProvider
        {
            private readonly object value;

            public CosmosDBMongoValueProvider(object value)
            {
                this.value = value;
            }

            public Type Type => typeof(ChangeStreamDocument<BsonDocument>);

            public Task<object> GetValueAsync()
            {
                return Task.FromResult(this.value);
            }

            public string ToInvokeString() => string.Empty;

            public Task SetValueAsync(object value, object cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}