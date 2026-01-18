// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET8_0_OR_GREATER
using Azure.Core;
using Azure.Identity;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth
{
    /// <summary>
    /// Handler for Microsoft Entra ID authentication.
    /// Supports System-assigned MI, User-assigned MI, Service Principal, and custom TokenCredential.
    /// </summary>
    public class EntraIdAuthHandler : IAuthHandler
    {
        private readonly string _tenantId;
        private readonly string _managedIdentityClientId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly TokenCredential _customCredential;

        /// <summary>
        /// Creates a new Entra ID auth handler.
        /// Priority: Service Principal > User-assigned MI > System-assigned MI / DefaultAzureCredential
        /// </summary>
        /// <param name="tenantId">Optional Azure AD tenant ID.</param>
        /// <param name="managedIdentityClientId">Optional client ID for User-assigned Managed Identity.</param>
        /// <param name="clientId">Optional Application (Client) ID for Service Principal authentication.</param>
        /// <param name="clientSecret">Optional Client Secret for Service Principal authentication.</param>
        public EntraIdAuthHandler(
            string tenantId = null, 
            string managedIdentityClientId = null,
            string clientId = null,
            string clientSecret = null)
        {
            _tenantId = tenantId;
            _managedIdentityClientId = managedIdentityClientId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _customCredential = null;
        }

        /// <summary>
        /// Creates a new Entra ID auth handler with a custom TokenCredential.
        /// </summary>
        public EntraIdAuthHandler(TokenCredential credential, string tenantId = null)
        {
            _customCredential = credential ?? throw new System.ArgumentNullException(nameof(credential));
            _tenantId = tenantId;
            _managedIdentityClientId = null;
            _clientId = null;
            _clientSecret = null;
        }

        public MongoClientSettings ConfigureAuth(string connectionString)
        {
            var parser = new ConnectionStringParser(connectionString);
            var preparedConnectionString = parser.PrepareForEntraIdAuth();
            var settings = MongoClientSettings.FromConnectionString(preparedConnectionString);

            // Configure OIDC credential with custom callback
            EntraIdOidcCallback oidcCallback;
            
            if (_customCredential != null)
            {
                // Use custom TokenCredential
                oidcCallback = new EntraIdOidcCallback(_customCredential, _tenantId);
            }
            else if (!string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret) && !string.IsNullOrEmpty(_tenantId))
            {
                // Use Service Principal (ClientSecretCredential)
                var credential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
                oidcCallback = new EntraIdOidcCallback(credential, _tenantId);
            }
            else
            {
                // Use DefaultAzureCredential (System MI, User MI, or local dev credentials)
                oidcCallback = new EntraIdOidcCallback(_tenantId, _managedIdentityClientId);
            }

            settings.Credential = MongoCredential.CreateOidcCredential(oidcCallback);

            // Best practice configuration for Azure DocumentDB (per official documentation)
            settings.UseTls = true;
            settings.RetryWrites = false;
            settings.MaxConnectionIdleTime = System.TimeSpan.FromMinutes(2);

            return settings;
        }
    }
}
#endif