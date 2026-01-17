// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Defines the authentication methods supported for connecting to Azure DocumentDB for MongoDB.
    /// </summary>
    public enum AuthMethod
    {
        /// <summary>
        /// Native MongoDB authentication.
        /// This is the default authentication method.
        /// </summary>
        NativeAuth = 0,

        /// <summary>
        /// Microsoft Entra ID authentication.
        /// Requires .NET 8.0 or later.
        /// </summary>
        MicrosoftEntraID = 1
    }
}