// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoTriggerBinding : ITriggerBinding
    {
        private readonly ParameterInfo _parameter;
        private readonly MongoCollectionReference _monitoredCollectionRef;
        private readonly ILogger _logger;

        public CosmosDBMongoTriggerBinding(ParameterInfo parameter, MongoCollectionReference monitoredCollectionRef, ILogger logger)
        {
            this._parameter = parameter;
            this._monitoredCollectionRef = monitoredCollectionRef;
            this._logger = logger;
        }

        public Type TriggerValueType => typeof(ChangeStreamDocument<BsonDocument>);

        public IReadOnlyDictionary<string, Type> BindingDataContract => new Dictionary<string, Type>
        {
            { "CosmosDBMongoTrigger", typeof(ChangeStreamDocument<BsonDocument>) }
        };

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var valueProvider = new CosmosDBMongoValueProvider(value);
            var bindingData = new Dictionary<string, object>
            {
                { "CosmosDBMongoTrigger", value }
            };
            return Task.FromResult<ITriggerData>(new TriggerData(valueProvider, bindingData));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Task.FromResult<IListener>(
                new CosmosDBMongoTriggerListener(context.Executor, this._monitoredCollectionRef, this._logger));
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