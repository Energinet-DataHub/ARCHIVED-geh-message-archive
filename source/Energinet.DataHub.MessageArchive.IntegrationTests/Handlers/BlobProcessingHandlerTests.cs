﻿using System;
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

            var cosmosMemoryStorage = new ArchivedParsedBlobItems();
            startup.Container.Register<IStorageWriter<CosmosRequestResponseLog>>(() => cosmosMemoryStorage);

            var scope = AsyncScopedLifestyle.BeginScope(startup.Container);
            var blobProcessingHandler = scope.GetInstance<IBlobProcessingHandler>();

            // -- Write test blob content to Storage
            await ReadAssetAndWriteToStorageForProcessingAsync("confirmrequestchangeofsupplier.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("requestchangeofsupplier.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("multiactivityrecords_confirmrequestchangeofsupplier.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("notifyValidatedMeasureData.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("rejectrequestchangeofsupplier.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("rejectRequestValidatedMeasureData.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("requestchangeofsupplier.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("requestValidatedMeasureData.json", "json").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("validation_exception.json", "json").ConfigureAwait(false);

            await ReadAssetAndWriteToStorageForProcessingAsync("notifybillingmasterdata.xml", "xml").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("requestchangeaccountingpointcharacteristics.xml", "xml").ConfigureAwait(false);
            await ReadAssetAndWriteToStorageForProcessingAsync("test-series-ids.xml", "xml").ConfigureAwait(false);

            // Act
            await blobProcessingHandler.HandleAsync().ConfigureAwait(false);

            // Assert
            Assert.NotNull(blobProcessingHandler);
            Assert.Equal(12, cosmosMemoryStorage.ParsedLogs.Count);
            Assert.True(cosmosMemoryStorage.ParsedLogs.All(r =>
                r.ParsingSuccess == true &&
                r.HaveBodyContent == true));

            await startup.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task ReadAssetAndWriteToStorageForProcessingAsync(string assertNameWithExtension, string contentType)
        {
            var filePathWithName = $"Assets/{assertNameWithExtension}";
            var logName = Guid.NewGuid().ToString();

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

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
