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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.EntryPoint;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;
using Energinet.DataHub.MessageArchive.Processing.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Handlers
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class BlobProcessingHandlerTests
    {
        [Fact]
        public async Task Test_ParseJson_CreateChangeOfSupplier()
        {
            // Arrange
            await IntegrationTestHelper.InitTestBlobStorageAsync(
                LocalSettings.StorageAccountConnectionString,
                LocalSettings.MessageArchiveContainerName).ConfigureAwait(false);
            await IntegrationTestHelper.InitTestBlobStorageAsync(
                LocalSettings.StorageAccountConnectionString,
                LocalSettings.MessageArchiveProcessedContainerName).ConfigureAwait(false);

            var startup = new Startup();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables()
                .Build());
            startup.ConfigureServices(serviceCollection);
            serviceCollection.BuildServiceProvider().UseSimpleInjector(
                startup.Container,
                x => x.Container.Options.EnableAutoVerification = false);
            startup.Container.Options.AllowOverridingRegistrations = true;

            var cosmosMemoryStorage = new MockedStorageWriter<CosmosRequestResponseLog>();
            startup.Container.Register<IStorageWriter<CosmosRequestResponseLog>>(() => cosmosMemoryStorage);

            var scope = AsyncScopedLifestyle.BeginScope(startup.Container);
            var blobProcessingHandler = scope.GetInstance<IBlobProcessingHandler>();

            // -- Write test blob content to Storage
            var assetsFilesWithPath = Directory.EnumerateFiles("Assets").ToList();

            foreach (var fileNameWithPath in assetsFilesWithPath)
            {
                await ReadAssetAndWriteToStorageForProcessingAsync(fileNameWithPath, fileNameWithPath[fileNameWithPath.LastIndexOf('.')..]).ConfigureAwait(false);
            }

            // Act
            await blobProcessingHandler.HandleAsync().ConfigureAwait(false);

            var cosmosStorage = cosmosMemoryStorage.Storage().ToList();

            // Assert
            Assert.Equal(assetsFilesWithPath.Count, cosmosStorage.Count);
            Assert.True(cosmosStorage.All(r =>
                r.ParsingSuccess == true &&
                r.HaveBodyContent == true));

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task ReadAssetAndWriteToStorageForProcessingAsync(string assertNameWithPathAndExtension, string contentType)
        {
            var logName = Guid.NewGuid().ToString();

            using var fileStream = File.Open(assertNameWithPathAndExtension, FileMode.Open);

            var blobServiceClientMarketoplogs = await IntegrationTestHelper
                .InitTestBlobStorageAsync(
                    LocalSettings.StorageAccountConnectionString,
                    LocalSettings.MessageArchiveContainerName).ConfigureAwait(false);
            var containerClient =
                blobServiceClientMarketoplogs.GetBlobContainerClient(LocalSettings.MessageArchiveContainerName);
            var blobClient = containerClient.GetBlobClient(logName);

            var metaData = new Dictionary<string, string>()
            {
                { "contentType", contentType },
                { "statuscode", "OK" },
                { "functionid", Guid.NewGuid().ToString() },
                { "functionname", "IntegrationTest" },
                { "invocationid", Guid.NewGuid().ToString() },
                { "traceparent", "00-c0154b200b82f64e935c4bcd579b5f58-19fe709ca4ff48ff-01" },
                { "traceid", "c0154b200b82f64e935c4bcd579b5f58" },
                { "httpdatatype", "request" },
                {
                    "indexTags", "{\"jwtactorid\":\"" + Guid.NewGuid() + "\"," +
                                 "\"functionid\":\"" + Guid.NewGuid() + "\"," +
                                 "\"functionname\":\"IntegrationTest\"," +
                                 "\"invocationid\":\"" + Guid.NewGuid() + "\"," +
                                 "\"traceparent\":\"00-c0154b200b82f64e935c4bcd579b5f58-19fe709ca4ff48ff-01\"," +
                                 "\"traceid\":\"c0154b200b82f64e935c4bcd579b5f58\"," +
                                 "\"httpdatatype\":\"request\"," +
                                 "\"uniquelogname\":\"" + logName + "\"," +
                                 "\"statuscode\":\"OK\"," +
                                 "\"correlationid\":\"" + Guid.NewGuid() + "\" }"
                },
            };
            var indexTags = new Dictionary<string, string>()
            {
                { "jwtactorid", Guid.NewGuid().ToString() },
                { "functionid", Guid.NewGuid().ToString() },
                { "functionname", "IntegrationTest" },
                { "invocationid", Guid.NewGuid().ToString() },
                { "traceparent", "00-c0154b200b82f64e935c4bcd579b5f58-19fe709ca4ff48ff-01" },
                { "traceid", "c0154b200b82f64e935c4bcd579b5f58" },
                { "httpdatatype", "request" },
                { "uniquelogname", logName },
                { "correlationid", Guid.NewGuid().ToString() },
            };

            var options = new BlobUploadOptions { Tags = indexTags, Metadata = metaData };

            // First Upload to storage
            await blobClient.UploadAsync(fileStream, options).ConfigureAwait(false);
        }
    }
}
