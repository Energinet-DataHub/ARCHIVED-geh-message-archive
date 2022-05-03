// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.FunctionApp.Diagnostics.HealthChecks;
using Energinet.DataHub.MessageArchive.Persistence;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.Persistence.Services;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;
using Energinet.DataHub.MessageArchive.Processing.Handlers;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Processing.Services;
using Energinet.DataHub.MessageArchive.Reader;
using Energinet.DataHub.MessageArchive.Reader.Factories;
using Energinet.DataHub.MessageArchive.Reader.Handlers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using Container = SimpleInjector.Container;

namespace Energinet.DataHub.MessageArchive.Common
{
    public abstract class StartupBase : IAsyncDisposable
    {
        protected StartupBase()
        {
            Container = new Container();
        }

        public Container Container { get; }

        public async ValueTask DisposeAsync()
        {
            await DisposeCoreAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            Configure(services);
            ConfigureSimpleInjector(services);

            // config
            var config = services.BuildServiceProvider().GetService<IConfiguration>();
            Container.Register(() => config!, Lifestyle.Singleton);

            // Health check
            services.AddScoped<IHealthCheckEndpointHandler, HealthCheckEndpointHandler>();
            services
                .AddHealthChecks()
                .AddLiveCheck()
                .AddAzureBlobStorage(config.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING"), config.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME"))
                .AddAzureBlobStorage(config.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING"), config.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME"))
                .AddCosmosDb(config.GetValue<string>("COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING"), "message-archive");

            // Add Application insights telemetry
            services.SetupApplicationInsightTelemetry(config ?? throw new InvalidOperationException());

            RegisterLogStreamReader(Container);
            RegisterBlobReader(Container);

            RegisterBlobArchiveProcessed(Container);
            RegisterCosmosStorageWriter(Container);

            Container.Register<IBlobProcessingHandler, BlobProcessingHandler>(Lifestyle.Transient);
            Container.Register<IArchiveSearchHandler, ArchiveSearchHandler>(Lifestyle.Scoped);
            Container.Register<IArchiveSearchRepository, ArchiveSearchRepository>(Lifestyle.Scoped);

            Configure(Container);
        }

        private static void RegisterLogStreamReader(Container container)
        {
            container.Register<IStorageStreamReader>(() =>
            {
                var configuration = container.GetService<IConfiguration>();

                var connectionString = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING");
                var containerName = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME");

                var factory = new StorageServiceClientFactory(connectionString);
                var storageConfig = new StorageConfig(containerName);

                return new BlobStorageStreamReader(factory, storageConfig);
            });
        }

        private static void RegisterBlobReader(Container container)
        {
            container.Register<IBlobReader>(() =>
            {
                var configuration = container.GetService<IConfiguration>();

                var connectionString = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING");
                var containerName = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME");
                return new BlobReader(connectionString, containerName);
            });
        }

        private static void RegisterBlobArchiveProcessed(Container container)
        {
            container.Register<IBlobArchive>(() =>
            {
                var configuration = container.GetService<IConfiguration>();

                var connectionString = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING");
                var fromContainerName = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME");
                var toContainerName = configuration.GetValue<string>("STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME");

                return new BlobArchive(connectionString, fromContainerName, toContainerName);
            });
        }

        private static void RegisterCosmosStorageWriter(Container container)
        {
            container.Register<IStorageWriter<CosmosRequestResponseLog>, ArchiveWriterRepository>();
            container.RegisterSingleton(() => GetCosmosClient(container));
            container.Register<IArchiveContainer, ArchiveContainer>(Lifestyle.Scoped);
        }

        private static IArchiveCosmosClient GetCosmosClient(Container container)
        {
            var configuration = container.GetService<IConfiguration>();
            var connectionString = configuration.GetValue<string>("COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Please specify a valid CosmosDBConnection in the appSettings.json file or your Azure Functions Settings.");
            }

            var cosmosClient = new CosmosClientBuilder(connectionString)
                .WithSerializerOptions(new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                })
                .Build();

            return new ArchiveCosmosClient(cosmosClient);
        }

#pragma warning disable SA1202
        protected virtual ValueTask DisposeCoreAsync()
#pragma warning restore SA1202
        {
            return Container.DisposeAsync();
        }

        protected abstract void Configure(IServiceCollection services);

        protected abstract void ConfigureSimpleInjector(IServiceCollection services);

        protected abstract void Configure(Container container);
    }
}
