// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Config
{
    /// <summary>
    /// Encapsulates connection information for MongoDB connections.
    /// Supports both connection string-based and token credential-based authentication.
    /// </summary>
    internal class MongoDBConnectionInformation
    {
        /// <summary>
        /// Initializes a new instance using a connection string.
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string.</param>
        public MongoDBConnectionInformation(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            UsesConnectionString = true;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance using token credential (Entra ID authentication).
        /// </summary>
        /// <param name="accountEndpoint">The MongoDB account endpoint (e.g., myaccount.mongo.cosmos.azure.com:10255).</param>
        /// <param name="credential">The token credential for authentication.</param>
        public MongoDBConnectionInformation(string accountEndpoint, TokenCredential credential)
        {
            if (string.IsNullOrEmpty(accountEndpoint))
            {
                throw new ArgumentNullException(nameof(accountEndpoint));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            UsesConnectionString = false;
            AccountEndpoint = accountEndpoint;
            Credential = credential;
        }

        /// <summary>
        /// Gets a value indicating whether this connection uses a connection string.
        /// </summary>
        public bool UsesConnectionString { get; }

        /// <summary>
        /// Gets the connection string (only valid when UsesConnectionString is true).
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Gets the account endpoint (only valid when UsesConnectionString is false).
        /// </summary>
        public string AccountEndpoint { get; }

        /// <summary>
        /// Gets the token credential (only valid when UsesConnectionString is false).
        /// </summary>
        public TokenCredential Credential { get; }
    }
}
