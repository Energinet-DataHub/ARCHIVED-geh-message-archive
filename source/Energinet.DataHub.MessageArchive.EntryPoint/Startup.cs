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
using Energinet.DataHub.MessageArchive.EntryPoint.Functions;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository.Containers;
using Energinet.DataHub.MessageArchive.EntryPoint.SimpleInjector;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleInjector;
using Container = SimpleInjector.Container;

namespace Energinet.DataHub.MessageArchive.EntryPoint
{
    internal sealed class Startup : IAsyncDisposable
    {
        public Startup()
        {
            Container = new Container();
        }

        public Container Container { get; }

        public async ValueTask DisposeAsync()
        {
            await Container.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            SwitchToSimpleInjector(services);

            services.AddLogging();
            services.AddSimpleInjector(Container, x =>
            {
                x.DisposeContainerWithServiceProvider = !true;
                x.AddLogging();
            });

            // config
            var config = services.BuildServiceProvider().GetService<IConfiguration>();
            Container.RegisterSingleton(() => config!);

            Container.Register<ITestService, TestService>(Lifestyle.Transient);
            Container.Register<TriggerFunction>(Lifestyle.Scoped);
            Container.RegisterSingleton<IArchiveCosmosClient>(() => GetCosmosClient(Container));
            Container.Register<IArchiveContainer, ArchiveContainer>(Lifestyle.Scoped);
            Container.Register<IArchiveReaderRepository, ArchiveReaderRepository>(Lifestyle.Scoped);
        }

        private static IArchiveCosmosClient GetCosmosClient(Container container)
        {
            var configuration = container.GetService<IConfiguration>();
            var connectionString = configuration.GetValue<string>("MESSAGE_ARCHIVE_DB_CONNECTION_STRING");

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

        private static void SwitchToSimpleInjector(IServiceCollection services)
        {
            var descriptor = new ServiceDescriptor(
                typeof(IFunctionActivator),
                typeof(SimpleInjectorActivator),
                ServiceLifetime.Singleton);

            services.Replace(descriptor);
        }
    }
}
