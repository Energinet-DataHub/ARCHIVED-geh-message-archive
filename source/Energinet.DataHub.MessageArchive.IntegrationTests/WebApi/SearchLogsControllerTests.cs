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
using Energinet.DataHub.MessageArchive.EntryPoint;
using Energinet.DataHub.MessageArchive.EntryPoint.WebApi.Controllers;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;
using Energinet.DataHub.MessageArchive.Reader;
using Energinet.DataHub.MessageArchive.Reader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.WebApi
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class SearchLogsControllerTests
    {
        [Fact]
        public async Task Search_ResultFound()
        {
            // Arrange
            var (scope, startup) = RunStartUp();
            var logger = scope.GetInstance<ILogger<SearchLogsController>>();
            var archiveSearchRepository = scope.GetInstance<IArchiveSearchRepository>();
            var archiveWriter = scope.GetInstance<IStorageWriter<CosmosRequestResponseLog>>();

            var searchController = new SearchLogsController(logger, archiveSearchRepository);

            var messageTypeToGroupTest = Guid.NewGuid().ToString();
            var expectedResults = 5;
            await SeedDatabaseWithContent(archiveWriter, messageTypeToGroupTest, expectedResults).ConfigureAwait(false);

            var searchCriteria = new SearchCriteria()
            {
                MessageType = messageTypeToGroupTest,
                DateTimeFrom = "2022-01-01T00:00:00Z",
                DateTimeTo = "2022-05-01T23:59:59Z",
            };

            // Act
            var searchResultIActionResult = await searchController.SearchAsync(searchCriteria).ConfigureAwait(false);

            var jsonResult = (searchResultIActionResult as OkObjectResult)?.Value as string;
            var searchResult = (searchResultIActionResult as OkObjectResult)?.Value as SearchResults;

            // Assert
            Assert.NotNull(searchResult);
            Assert.Equal(expectedResults, searchResult.Result.Count);
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

        private static async Task SeedDatabaseWithContent(IStorageWriter<CosmosRequestResponseLog> archiveWriter, string messageType, int numberToAdd)
        {
            var createdDateParsed = DateTimeOffset.TryParse("2022-04-05T00:00:00Z", out var createdDataValueParsed);

            for (var i = 0; i < numberToAdd; i++)
            {
                var content = new CosmosRequestResponseLog()
                {
                    MessageType = messageType,
                    CreatedDate = createdDateParsed ? createdDataValueParsed : null,
                    LogCreatedDate = createdDateParsed ? createdDataValueParsed : null,
                };
                await archiveWriter.WriteAsync(content).ConfigureAwait(false);
            }
        }
    }
}
