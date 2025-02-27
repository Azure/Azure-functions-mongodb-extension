using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public static class CosmosDBMongoConstant
    {
        internal const string AzureFunctionApplicationName = "AzureCosmosDBMongoExtension";
        internal const string AzureFunctionTelemetryCategory = "CosmosDBMongo";
    }

    internal static class Events
    {
        public static readonly EventId OnError = new EventId(1000, "OnTriggerError");
        public static readonly EventId OnIntializedCollection = new EventId(2000, "OnIntializedCollection");
        public static readonly EventId OnBindingOutputDataAdded = new EventId(3000, "OnBindingDataAdded");
        public static readonly EventId OnBindingOutputDataError = new EventId(3010, "OnBindingDataError");
        public static readonly EventId OnBindingInputQuery = new EventId(4000, "OnBindingInputQuery");
        public static readonly EventId OnBindingInputQueryError = new EventId(4010, "OnBindingInputQueryError");
        public static readonly EventId OnScaling = new EventId(5000, "OnScaling");
        public static readonly EventId OnListenerStarted  = new EventId(6000, "OnListenerStarted");
        public static readonly EventId OnListenerStopped = new EventId(7000, "OnListenerStopped");
        public static readonly EventId OnListenerStartError  = new EventId(6010, "OnListenerStartError ");
        public static readonly EventId OnListenerStopError = new EventId(7010, "OnListenerStopError");
    }
}
