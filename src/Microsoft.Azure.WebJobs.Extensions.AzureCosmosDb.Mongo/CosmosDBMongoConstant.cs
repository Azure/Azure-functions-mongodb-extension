using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public static class CosmosDBMongoConstant
    {
        public const string DefaultConnectionStringKey = "CosmosDBMongo";
        internal const string AzureFunctionApplicationName = "AzureCosmosDBMongoExtension";
        internal const string AzureFunctionTelemetryCategory = "CosmosDBMongo";

        // internal const int NumberOfSamplesToConsiderForScaling = 5;
    }

    internal static class Events
    {
        public static readonly EventId OnError = new EventId(1000, "OnTriggerError");
        public static readonly EventId OnIntializedCollection = new EventId(2000, "OnIntializedCollection");
        public static readonly EventId OnBindingDataAdded = new EventId(3000, "OnBindingDataAdded");
        public static readonly EventId OnBindingDataError = new EventId(4000, "OnBindingDataError");
        // public static readonly EventId OnScalingOut = new EventId(5000, "OnScalingOut");
        // public static readonly EventId OnScalingIn = new EventId(5010, "OnScalingIn");
        // public static readonly EventId ServiceSteady = new EventId(5020, "ServiceSteady");
        // public static readonly EventId OnScalingError = new EventId(5030, "OnScalingError");
        public static readonly EventId OnListenerStarted  = new EventId(6000, "OnListenerStarted");
        public static readonly EventId OnListenerStopped = new EventId(7000, "OnListenerStopped");
        public static readonly EventId OnListenerStartError  = new EventId(8000, "OnListenerStartError ");
        public static readonly EventId OnListenerStopError = new EventId(9000, "OnListenerStopError");
    }
}
