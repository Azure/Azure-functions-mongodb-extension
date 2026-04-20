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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
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
            IValueProvider valueProvider;
            if (_parameter.ParameterType == typeof(string))
            {
                valueProvider = new CosmosDBMongoStringValueProvider(value);
            }
            else
            {
                valueProvider = new CosmosDBMongoValueProvider(value);
            }

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
            return new CosmosDBMongoTriggerParameterDescriptor
            {
                Name = "CosmosDBMongoTrigger",
                FunctionName = _monitoredCollectionRef.functionId,
                DatabaseName = _monitoredCollectionRef.databaseName,
                CollectionName = _monitoredCollectionRef.collectionName,
                LeaseConnectionStringSetting = _monitoredCollectionRef.leaseConnectionStringSetting,
                LeaseDatabaseName = _monitoredCollectionRef.leaseDatabaseName,
                LeaseCollectionName = _monitoredCollectionRef.leaseCollectionName,
                LeaseTenantId = _monitoredCollectionRef.leaseTenantId,
                LeaseManagedIdentityClientId = _monitoredCollectionRef.leaseManagedIdentityClientId
            };
        }

        internal class CosmosDBMongoTriggerParameterDescriptor : TriggerParameterDescriptor
        {
            public string FunctionName { get; set; }
            public string DatabaseName { get; set; }
            public string CollectionName { get; set; }
            public string LeaseConnectionStringSetting { get; set; }
            public string LeaseDatabaseName { get; set; }
            public string LeaseCollectionName { get; set; }
            public string LeaseTenantId { get; set; }
            public string LeaseManagedIdentityClientId { get; set; }

            public override string GetTriggerReason(IDictionary<string, string> arguments)
            {
                return string.Format("CosmosDB Mongo trigger fired at {0}", DateTime.Now.ToString("o"));
            }
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

            public string ToInvokeString()
            {
                if (this.value is ChangeStreamDocument<BsonDocument> changeStreamDoc)
                {
                    return changeStreamDoc.BackingDocument.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
                }

                return string.Empty;
            }

            public Task SetValueAsync(object value, object cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private class CosmosDBMongoStringValueProvider : IValueProvider
        {
            private readonly object value;

            public CosmosDBMongoStringValueProvider(object value)
            {
                this.value = value;
            }

            public Type Type => typeof(string);

            public Task<object> GetValueAsync()
            {
                if (this.value is ChangeStreamDocument<BsonDocument> changeStreamDoc)
                {
                    string json = changeStreamDoc.BackingDocument.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
                    return Task.FromResult<object>(json);
                }

                return Task.FromResult<object>(this.value?.ToString() ?? string.Empty);
            }

            public string ToInvokeString()
            {
                if (this.value is ChangeStreamDocument<BsonDocument> changeStreamDoc)
                {
                    return changeStreamDoc.BackingDocument.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
                }

                return string.Empty;
            }

            public Task SetValueAsync(object value, object cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}