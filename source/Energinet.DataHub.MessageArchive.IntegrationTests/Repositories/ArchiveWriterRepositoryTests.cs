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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;
using Energinet.DataHub.MessageArchive.Reader;
using Energinet.DataHub.MessageArchive.Reader.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class ArchiveWriterRepositoryTests
    {
        [Fact]
        public async Task SaveToStorage_Ok()
        {
            // Arrange
            var (scope, startup) = RunStartUp();
            var archiveWriter = scope.GetInstance<IStorageWriter<CosmosRequestResponseLog>>();
            var archiveSearchRepository = scope.GetInstance<IArchiveSearchRepository>();

            var messageType = Guid.NewGuid().ToString();

            var expected = await AddDataToDb(archiveWriter, messageType).ConfigureAwait(false);

            // Act
            var searchCriteria = BuildCriteria(messageType, "notifybillingmasterdata");
            var result = await archiveSearchRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected.Count, result.Result.Count);
            Assert.Equal(expected.Any(e => e.MessageType != messageType), result.Result.Any(e => e.MessageType != messageType));

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task<List<CosmosRequestResponseLog>> AddDataToDb(IStorageWriter<CosmosRequestResponseLog> archiveWriter, string messageType)
        {
            var data = new List<CosmosRequestResponseLog>();
            data.Add(CreateCosmosRequestResponseLog(
                "1",
                messageType,
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "2",
                messageType,
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "3",
                messageType,
                "notifybillingmasterdata"));

            foreach (var sample in data)
            {
                await archiveWriter.WriteAsync(sample).ConfigureAwait(false);
            }

            return data;
        }

        private static CosmosRequestResponseLog CreateCosmosRequestResponseLog(
            string messageId,
            string messageType,
            string rsmName)
        {
            var model = new CosmosRequestResponseLog
            {
                Id = messageId,
                PartitionKey = Guid.NewGuid().ToString(),
                MessageId = messageId,
                MessageType = messageType,
                RsmName = rsmName,
            };
            return model;
        }

        private static (Scope scope, Startup startup) RunStartUp()
        {
            var startup = new Startup();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables()
                .Build());
            startup.ConfigureServices(serviceCollection);
            serviceCollection.BuildServiceProvider().UseSimpleInjector(
                startup.Container,
                x => x.Container.Options.EnableAutoVerification = false);
            startup.Container.Options.AllowOverridingRegistrations = true;
            var scope = AsyncScopedLifestyle.BeginScope(startup.Container);
            return (scope, startup);
        }

        private static SearchCriteria BuildCriteria(string messageType, string rsmName)
        {
            return new SearchCriteria(
                null,
                messageType,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                rsmName);
        }
    }
}
