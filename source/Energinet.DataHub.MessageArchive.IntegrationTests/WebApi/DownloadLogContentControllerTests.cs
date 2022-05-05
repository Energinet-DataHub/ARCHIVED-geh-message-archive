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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.WebApi;
using Energinet.DataHub.MessageArchive.EntryPoint.WebApi.Controllers;
using Energinet.DataHub.MessageArchive.Reader;
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
    public class DownloadLogContentControllerTests
    {
        [Fact]
        public async Task Download_ContentStream_Ok()
        {
            // Arrange
            var connectionString = "UseDevelopmentStorage=true;";
            var marketoplogsArchive = "marketoplogs-archive";

            var (scope, startup) = RunSetup(connectionString, marketoplogsArchive);
            var logger = scope.GetInstance<ILogger<DownloadLogContentController>>();
            var storageStreamReader = scope.GetInstance<IStorageStreamReader>();
            var downloadController = new DownloadLogContentController(logger, storageStreamReader);

            string logNameToDownload = Guid.NewGuid().ToString();
            var logContent = "some_cim_data";
            await using var logNameToDownloadContentStream = new MemoryStream(Encoding.UTF8.GetBytes(logContent));

            await SetupDatabaseAndDataAsync(connectionString, marketoplogsArchive, logNameToDownload, logNameToDownloadContentStream).ConfigureAwait(false);

            // Act
            var result = await downloadController.DownloadAsync(logNameToDownload).ConfigureAwait(false);
            var streamContent = result as FileStreamResult;
            using var streamContentReader = new StreamReader(streamContent?.FileStream ?? Stream.Null);
            var contentAsString = await streamContentReader.ReadToEndAsync().ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(logNameToDownload, streamContent?.FileDownloadName, StringComparison.InvariantCultureIgnoreCase);
            Assert.Equal(logContent, contentAsString);
        }

        private static async Task SetupDatabaseAndDataAsync(string connectionString, string containerName, string blobName, Stream logContent)
        {
            var client = await IntegrationTestHelper.InitTestBlobStorageAsync(connectionString, containerName).ConfigureAwait(false);
            var containerClient = client.GetBlobContainerClient(containerName);
            await containerClient.UploadBlobAsync(blobName, logContent).ConfigureAwait(false);
        }

        private static (Scope Scope, Startup Startup) RunSetup(string storageConnectionString, string storageContainerName)
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            configuration["FRONTEND_OPEN_ID_URL"] = "value";
            configuration["FRONTEND_SERVICE_APP_ID"] = "value";
            configuration["STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING"] = storageConnectionString;
            configuration["STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME"] = storageContainerName;
            var startup = new Startup(configuration);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            startup.ConfigureServices(serviceCollection);
            serviceCollection.BuildServiceProvider().UseSimpleInjector(
                startup.Container,
                x => x.Container.Options.EnableAutoVerification = false);
            startup.Container.Options.AllowOverridingRegistrations = true;
            var scope = AsyncScopedLifestyle.BeginScope(startup.Container);
            return (scope, startup);
        }
    }
}
