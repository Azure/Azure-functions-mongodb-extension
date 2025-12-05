// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public static class CosmosDBMongoConstant
    {
        public const string DefaultConnectionStringKey = "CosmosDBMongo";
        internal const string AzureFunctionApplicationName = "AzureCosmosDBMongoExtension";
        internal const string AzureFunctionTelemetryCategory = "CosmosDBMongo";
    }

    internal static class Events
    {
        public static readonly EventId OnError = new EventId(1000, "OnTriggerError");
        public static readonly EventId OnIntializedCollection = new EventId(2000, "OnIntializedCollection");
        public static readonly EventId OnBindingDataAdded = new EventId(3000, "OnBindingDataAdded");
        public static readonly EventId OnBindingDataError = new EventId(4000, "OnBindingDataError");
        public static readonly EventId OnScalingOut = new EventId(5000, "OnScalingOut");
        public static readonly EventId OnScalingIn = new EventId(5010, "OnScalingIn");
        public static readonly EventId ServiceSteady = new EventId(5020, "ServiceSteady");
        public static readonly EventId OnScalingError = new EventId(5030, "OnScalingError");
        public static readonly EventId OnListenerStarted  = new EventId(6000, "OnListenerStarted");
        public static readonly EventId OnListenerStopped = new EventId(7000, "OnListenerStopped");
        public static readonly EventId OnListenerStartError  = new EventId(8000, "OnListenerStartError ");
        public static readonly EventId OnListenerStopError = new EventId(9000, "OnListenerStopError");
        public static readonly EventId OnChangeStreamError = new EventId(10000, "OnChangeStreamError");
        public static readonly EventId OnChangeStreamReconnecting = new EventId(10010, "OnChangeStreamReconnecting");
        public static readonly EventId OnChangeStreamReconnected = new EventId(10020, "OnChangeStreamReconnected");
    }
}
