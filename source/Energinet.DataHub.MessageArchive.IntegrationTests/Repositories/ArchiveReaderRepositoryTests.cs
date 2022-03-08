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
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class ArchiveReaderRepositoryTests
    {
        [Fact]
        public async Task GetSearchResultsAsync_SearchRequest_ValidDataReturned()
        {
            // Arrange
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
            var archiveReaderRepository = scope.GetInstance<IArchiveReaderRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var expected = await AddDataToDb(archiveContainer).ConfigureAwait(false);

            var searchCriteria = new SearchCriteria(
                null,
                null,
                null,
                "2020-01-01",
                "2020-04-04",
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
                null);

            // Act
            var result = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected[0].MessageId, result.Result[0].MessageId);
            Assert.Equal(expected[0].MessageType, result.Result[0].MessageType);
            Assert.Equal(expected[0].ProcessType, result.Result[0].ProcessType);
            Assert.Equal(expected[0].SenderGln, result.Result[0].SenderGln);
            Assert.Equal(expected[0].ReasonCode, result.Result[0].ReasonCode);
            Assert.Equal(expected[0].RsmName, result.Result[0].RsmName);

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_IncludeRelated()
        {
            // Arrange
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
            var archiveReaderRepository = scope.GetInstance<IArchiveReaderRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var expected = await AddDataToDb(archiveContainer).ConfigureAwait(false);
            var related = CreateCosmosRequestResponseLog("12345", "related", "process", "senderGln", "reasonCode", "rsmName");
            related.OriginalTransactionIDReferenceId = "1";
            await archiveContainer.Container.UpsertItemAsync(related).ConfigureAwait(false);
            expected.Add(related);

            var searchCriteria = new SearchCriteria(
                "1",
                null,
                null,
                "2020-01-01",
                "2020-04-04",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "1",
                null);

            // Act
            var result = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            var relatedDbResult = result.Result.FirstOrDefault(e => e.MessageId == related.MessageId);

            Assert.NotNull(relatedDbResult);
            Assert.Equal(related.MessageId, relatedDbResult.MessageId);
            Assert.Equal(related.OriginalTransactionIDReferenceId, result.Result[0].MessageId);
            Assert.NotEqual(relatedDbResult.MessageId, result.Result[0].MessageId);

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task<List<CosmosRequestResponseLog>> AddDataToDb(IArchiveContainer container)
        {
            var data = new List<CosmosRequestResponseLog>();
            data.Add(CreateCosmosRequestResponseLog(
                "1",
                "message",
                "1",
                "fake_value",
                "fake_value",
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "2",
                "message",
                "2",
                "fake_value",
                "fake_value",
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "3",
                "message",
                "3",
                "fake_value",
                "fake_value",
                "notifybillingmasterdata"));

            foreach (var sample in data)
            {
                await container.Container.UpsertItemAsync(sample).ConfigureAwait(false);
            }

            return data;
        }

        private static CosmosRequestResponseLog CreateCosmosRequestResponseLog(
            string messageId,
            string messageType,
            string processType,
            string senderGln,
            string reasonCode,
            string rsmName)
        {
            var model = new CosmosRequestResponseLog();
            model.Id = messageId;
            model.PartitionKey = Guid.NewGuid().ToString();
            model.MessageId = messageId;
            model.MessageType = messageType;
            model.ProcessType = processType;
            model.SenderGln = senderGln;
            model.ReasonCode = reasonCode;
            model.RsmName = rsmName;
            return model;
        }
    }
}
