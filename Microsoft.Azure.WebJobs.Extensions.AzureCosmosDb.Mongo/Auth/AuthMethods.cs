// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Defines the authentication methods supported for connecting to Azure DocumentDB for MongoDB.
    /// This enum is used internally - the authentication method is auto-detected based on TenantId:
    /// - If TenantId is specified → MicrosoftEntraID
    /// - If TenantId is not specified → NativeAuth
    /// </summary>
    internal enum AuthMethod
    {
        /// <summary>
        /// Native MongoDB authentication using connection string credentials.
        /// Used when TenantId is not specified.
        /// </summary>
        NativeAuth = 0,

        /// <summary>
        /// Microsoft Entra ID authentication.
        /// Used when TenantId is specified.
        /// Requires .NET 8.0 or later.
        /// </summary>
        MicrosoftEntraID = 1
    }
}