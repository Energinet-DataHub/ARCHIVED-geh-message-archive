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
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.PersistenceModels;
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
            var archiveSearchRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var testGroup = Guid.NewGuid().ToString();
            var expected = await AddDataToDb(archiveContainer, testGroup).ConfigureAwait(false);

            var searchCriteria = GetSearchCriteria(null, testGroup, false);

            // Act
            var result = await archiveSearchRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

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
        public async Task GetSearchResultsAsync_RsmNamesAndProcessTypes()
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
            var archiveSearchRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var rsmProcessTypes = new List<(string RsmName, string ProcessType)>
            {
                ("notifybillingmasterdata", "D1"),
                ("requestchangeaccountingpointcharacteristics", "D5"),
                ("rejectrequestchangeaccountingpointcharacteristics", "D3"),
                ("requestchangeaccountingpointcharacteristics", "D6"),
                ("genericnotification", "D5"),
                ("genericnotification", "D6"),
                ("genericnotification", "D6"),
                ("genericnotification", "D11"),
            };

            var testGroup = Guid.NewGuid().ToString();
            await AddDataToDbForRsmNamesAndProcessTypes(archiveContainer, testGroup, rsmProcessTypes).ConfigureAwait(false);

            var searchCriteria = GetSearchCriteria(null, testGroup, false);
            searchCriteria.RsmNames = new List<string>() { "genericnotification", "requestchangeaccountingpointcharacteristics" };
            searchCriteria.ProcessTypes = new List<string>() { "D6", "D5" };

            // Act
            var result = await archiveSearchRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            Assert.True(result.Result.Count == 5);

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_IncludeRelated_In()
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
            var archiveReaderRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var testGroup = Guid.NewGuid().ToString();

            await AddDataToDb(archiveContainer, Guid.NewGuid().ToString()).ConfigureAwait(false);

            var messageIdIn = Guid.NewGuid().ToString();
            var mridTransaction1 = Guid.NewGuid().ToString();

            var logWithReferenceIn = CreateCosmosRequestResponseLog(messageIdIn, testGroup, "rsmName");
            logWithReferenceIn.HttpData = "request";
            logWithReferenceIn.TransactionRecords = new List<TransactionRecord>
            {
                new()
                {
                    MRid = mridTransaction1,
                    OriginalTransactionIdReferenceId = Guid.NewGuid().ToString(),
                },
                new()
                {
                    MRid = Guid.NewGuid().ToString(),
                    OriginalTransactionIdReferenceId = Guid.NewGuid().ToString(),
                },
            };
            await archiveContainer.Container.UpsertItemAsync(logWithReferenceIn).ConfigureAwait(false);

            var logWithReferenceOut = CreateCosmosRequestResponseLog(Guid.NewGuid().ToString(), testGroup, "rsmName");
            logWithReferenceOut.HttpData = "response";
            logWithReferenceOut.TransactionRecords = new List<TransactionRecord>
            {
                new()
                {
                    MRid = Guid.NewGuid().ToString(),
                    OriginalTransactionIdReferenceId = mridTransaction1,
                },
            };
            await archiveContainer.Container.UpsertItemAsync(logWithReferenceOut).ConfigureAwait(false);

            var searchCriteria = GetSearchCriteria(messageIdIn, testGroup, true);

            // Act
            var result = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            var inDbResult = result.Result.FirstOrDefault(e => e.MessageId == messageIdIn);

            Assert.NotNull(inDbResult);
            Assert.True(result.Result.Count == 2);
            Assert.Contains(result.Result, e => e.TransactionRecords != null && e.TransactionRecords.Any(r => r.MRid == mridTransaction1));
            Assert.Contains(result.Result, e => e.TransactionRecords != null && e.TransactionRecords.Any(r => r.OriginalTransactionIdReferenceId == mridTransaction1));

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_IncludeRelated_Out()
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
            var archiveReaderRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var testGroup = Guid.NewGuid().ToString();

            var messageIdIn = Guid.NewGuid().ToString();
            var messageIdOut = Guid.NewGuid().ToString();

            var mridTransaction1 = Guid.NewGuid().ToString();
            var mridTransaction2 = Guid.NewGuid().ToString();

            var logWithReferenceOut = CreateCosmosRequestResponseLog(messageIdOut, testGroup, "rsmName");
            logWithReferenceOut.HttpData = "response";
            logWithReferenceOut.TransactionRecords = new List<TransactionRecord>
            {
                new()
                {
                    MRid = Guid.NewGuid().ToString(),
                    OriginalTransactionIdReferenceId = mridTransaction1,
                },
                new()
                {
                    MRid = Guid.NewGuid().ToString(),
                    OriginalTransactionIdReferenceId = mridTransaction2,
                },
            };
            await archiveContainer.Container.UpsertItemAsync(logWithReferenceOut).ConfigureAwait(false);

            var logWithReferenceIn = CreateCosmosRequestResponseLog(messageIdIn, testGroup, "rsmName");
            logWithReferenceIn.HttpData = "request";
            logWithReferenceIn.TransactionRecords = new List<TransactionRecord>
            {
                new()
                {
                    MRid = mridTransaction1,
                },
                new()
                {
                    MRid = mridTransaction2,
                },
            };
            await archiveContainer.Container.UpsertItemAsync(logWithReferenceIn).ConfigureAwait(false);

            var searchCriteria = GetSearchCriteria(messageIdOut, testGroup, true);

            // Act
            var searchResult = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            var inDbResult = searchResult.Result.FirstOrDefault(e => e.MessageId == messageIdIn);
            var outDbResult = searchResult.Result.FirstOrDefault(e => e.MessageId == messageIdOut);

            Assert.NotNull(inDbResult);
            Assert.NotNull(outDbResult);
            Assert.True(searchResult.Result.Count == 2);
            if (inDbResult.TransactionRecords != null)
            {
                Assert.Contains(
                    inDbResult.TransactionRecords.Select(e => e.MRid),
                    r => outDbResult.TransactionRecords != null && outDbResult.TransactionRecords.Select(t => t.OriginalTransactionIdReferenceId).Contains(r));
            }

            if (outDbResult.TransactionRecords != null)
            {
                Assert.Contains(
                    outDbResult.TransactionRecords.Select(e => e.OriginalTransactionIdReferenceId),
                    r => inDbResult.TransactionRecords != null && inDbResult.TransactionRecords.Select(t => t.MRid).Contains(r));
            }

            Assert.Contains(
                searchResult.Result,
                e => e.TransactionRecords != null && e.TransactionRecords.Any(r => r.MRid == mridTransaction1));
            Assert.Contains(
                searchResult.Result,
                e => e.TransactionRecords != null &&
                     e.TransactionRecords.Any(r => r.OriginalTransactionIdReferenceId == mridTransaction1));

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_CosmosPaging()
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
            var archiveReaderRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveContainer = scope.GetInstance<IArchiveContainer>();

            var testGroupMessageType = Guid.NewGuid().ToString();
            var insertCount = 30;
            var addedData = await AddDataToDbForPaging(archiveContainer, testGroupMessageType, insertCount).ConfigureAwait(false);

            var searchCriteria = GetSearchCriteria("pagingMessageId", testGroupMessageType, false);
            searchCriteria.MaxItemCount = 10;

            // Act
            var resultFirstPage = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            searchCriteria.ContinuationToken = resultFirstPage.ContinuationToken;
            var resultSecondPage = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            searchCriteria.ContinuationToken = resultSecondPage.ContinuationToken;
            var resultThirdPage = await archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            // Assert
            Assert.NotNull(resultFirstPage.ContinuationToken);
            Assert.NotEmpty(resultFirstPage.Result);
            Assert.Equal(10, resultFirstPage.Result.Count);

            Assert.NotNull(resultSecondPage.ContinuationToken);
            Assert.NotEmpty(resultSecondPage.Result);
            Assert.Equal(10, resultSecondPage.Result.Count);

            Assert.Null(resultThirdPage.ContinuationToken);
            Assert.NotEmpty(resultThirdPage.Result);
            Assert.Equal(10, resultThirdPage.Result.Count);

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        private static SearchCriteria GetSearchCriteria(string? messageId, string messageType, bool includeRelated)
        {
            var searchCriteria = new SearchCriteria(
                messageId,
                messageType,
                new List<string>(),
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
                includeRelated,
                new List<string>());
            searchCriteria.IncludeResultsWithoutContent = true;
            return searchCriteria;
        }

        private static async Task<List<CosmosRequestResponseLog>> AddDataToDb(IArchiveContainer container, string testGroup)
        {
            var data = new List<CosmosRequestResponseLog>();
            data.Add(CreateCosmosRequestResponseLog(
                "1",
                testGroup,
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "2",
                testGroup,
                "notifybillingmasterdata"));

            data.Add(CreateCosmosRequestResponseLog(
                "3",
                testGroup,
                "notifybillingmasterdata"));

            foreach (var sample in data)
            {
                await container.Container.UpsertItemAsync(sample).ConfigureAwait(false);
            }

            return data;
        }

        private static async Task<List<CosmosRequestResponseLog>> AddDataToDbForPaging(IArchiveContainer container, string groupMessageType, int insertCount)
        {
            var data = new List<CosmosRequestResponseLog>();

            for (var i = 0; i < insertCount; i++)
            {
                var elem = CreateCosmosRequestResponseLog("pagingMessageId", groupMessageType, "MasterData");
                data.Add(elem);
                await container.Container.CreateItemAsync(elem).ConfigureAwait(false);
            }

            return data;
        }

        private static async Task<List<CosmosRequestResponseLog>> AddDataToDbForRsmNamesAndProcessTypes(
            IArchiveContainer container,
            string testGroup,
            List<(string RsmName, string ProcessType)> rsmAndProcessType)
        {
            var data = new List<CosmosRequestResponseLog>();

            foreach (var (rsmName, processType) in rsmAndProcessType)
            {
                var elem = CreateCosmosRequestResponseLog(Guid.NewGuid().ToString(), testGroup, rsmName);
                elem.ProcessType = processType;
                data.Add(elem);
                await container.Container.CreateItemAsync(elem).ConfigureAwait(false);
            }

            return data;
        }

        private static CosmosRequestResponseLog CreateCosmosRequestResponseLog(
            string messageId,
            string messageType,
            string rsmName)
        {
            var createdDateParsed = DateTimeOffset.TryParse("2022-03-01T00:00:00Z", out var createdDataValueParsed);

            var model = new CosmosRequestResponseLog
            {
                Id = messageId,
                PartitionKey = Guid.NewGuid().ToString(),
                MessageId = messageId,
                MessageType = messageType,
                RsmName = rsmName,
                CreatedDate = createdDataValueParsed,
                LogCreatedDate = createdDataValueParsed,
                HaveBodyContent = true,
            };
            return model;
        }
    }
}
