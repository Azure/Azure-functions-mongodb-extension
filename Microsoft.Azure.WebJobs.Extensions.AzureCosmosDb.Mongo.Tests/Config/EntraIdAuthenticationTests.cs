// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests.Config
{
    [TestClass]
    public class EntraIdAuthenticationTests
    {
        [TestMethod]
        public void AddCosmosDBMongo_RegistersRequiredServices()
        {
            // Arrange
            var builder = new HostBuilder()
                .ConfigureWebJobs(b =>
                {
                    b.AddCosmosDBMongo();
                });

            // Act
            var host = builder.Build();
            var configProvider = host.Services.GetService<CosmosDBMongoConfigProvider>();
            var factory = host.Services.GetService<ICosmosDBMongoBindingCollectorFactory>();

            // Assert
            Assert.IsNotNull(configProvider, "CosmosDBMongoConfigProvider should be registered");
            Assert.IsNotNull(factory, "ICosmosDBMongoBindingCollectorFactory should be registered");
        }

        [TestMethod]
        public void CreateClient_WithConnectionString_Succeeds()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                { "CosmosDBMongo", "mongodb://testhost:27017" }
            };

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(configData);
                })
                .ConfigureWebJobs(b =>
                {
                    b.AddCosmosDBMongo();
                });

            // Act
            var host = builder.Build();
            var factory = host.Services.GetRequiredService<ICosmosDBMongoBindingCollectorFactory>();
            var client = factory.CreateClient("CosmosDBMongo");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void CreateClient_WithConnectionStringInConnectionStrings_Succeeds()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                { "ConnectionStrings:CosmosDBMongo", "mongodb://testhost:27017" }
            };

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(configData);
                })
                .ConfigureWebJobs(b =>
                {
                    b.AddCosmosDBMongo();
                });

            // Act
            var host = builder.Build();
            var factory = host.Services.GetRequiredService<ICosmosDBMongoBindingCollectorFactory>();
            var client = factory.CreateClient("CosmosDBMongo");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void CreateClient_WithDefaultConnectionString_Succeeds()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                { "CosmosDBMongo", "mongodb://defaulthost:27017" }
            };

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(configData);
                })
                .ConfigureWebJobs(b =>
                {
                    b.AddCosmosDBMongo();
                });

            // Act
            var host = builder.Build();
            var factory = host.Services.GetRequiredService<ICosmosDBMongoBindingCollectorFactory>();
            var client = factory.CreateClient(null);

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void CreateClient_WithMissingConnection_ThrowsException()
        {
            // Arrange
            var builder = new HostBuilder()
                .ConfigureWebJobs(b =>
                {
                    b.AddCosmosDBMongo();
                });

            // Act & Assert
            var host = builder.Build();
            var factory = host.Services.GetRequiredService<ICosmosDBMongoBindingCollectorFactory>();
            factory.CreateClient("NonExistentConnection");
        }
    }
}
