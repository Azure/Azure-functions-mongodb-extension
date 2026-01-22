// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
#if NET8_0_OR_GREATER
using Azure.Core;
#endif

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Auth
{
    public static class AuthHandlerFactory
    {
        /// <summary>
        /// Creates an auth handler based on the specified auth method.
        /// </summary>
        /// <param name="authMethod">The authentication method to use.</param>
        /// <param name="tenantId">Optional Azure AD tenant ID (only used for MicrosoftEntraID).</param>
        /// <param name="managedIdentityClientId">Optional client ID for User-assigned Managed Identity.</param>
        // /// <param name="clientId">Optional Application (Client) ID for Service Principal authentication.</param>
        // /// <param name="clientSecret">Optional Client Secret for Service Principal authentication.</param>
        /// <returns>An authentication handler.</returns>
        public static IAuthHandler Create(
            AuthMethod authMethod, 
            string tenantId = null, 
            string managedIdentityClientId = null)
            // string clientId = null,
            // string clientSecret = null)
        {
            switch (authMethod)
            {
                case AuthMethod.MicrosoftEntraID:
#if NET8_0_OR_GREATER
                    return new EntraIdAuthHandler(tenantId, managedIdentityClientId);
#else
                    throw new PlatformNotSupportedException(
                        "Microsoft Entra ID authentication is only supported on .NET 8.0 or later. " +
                        "Please use NativeAuth or upgrade to .NET 8.0.");
#endif

                case AuthMethod.NativeAuth:
                default:
                    return new NativeAuthHandler();
            }
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Creates an Entra ID auth handler with a custom TokenCredential.
        /// Use this for advanced scenarios.
        /// </summary>
        /// <param name="credential">The token credential to use for authentication.</param>
        /// <param name="tenantId">Optional Azure AD tenant ID.</param>
        /// <returns>An Entra ID authentication handler.</returns>
        public static IAuthHandler CreateEntraId(TokenCredential credential, string tenantId = null)
        {
            return new EntraIdAuthHandler(credential, tenantId);
        }
#endif
    }
}