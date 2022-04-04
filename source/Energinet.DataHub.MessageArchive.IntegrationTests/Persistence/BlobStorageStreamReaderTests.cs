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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Persistence.Services;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Reader.Factories;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Persistence
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class BlobStorageStreamReaderTests
    {
        [Fact]
        public async Task Test_BlobStorageStreamReader()
        {
            // Arrange
            var connnectionString = "UseDevelopmentStorage=true;";
            var marketoplogsArchive = "marketoplogs-archive";

            var storageFactory = new StorageServiceClientFactory(connnectionString);
            var blobStorageStreamReader = new BlobStorageStreamReader(storageFactory, new StorageConfig(marketoplogsArchive));

            var addedLog = await AddBlobToStorage(connnectionString, marketoplogsArchive).ConfigureAwait(false);

            // Act
            var stream = await blobStorageStreamReader.GetStreamFromStorageAsync(addedLog.Name).ConfigureAwait(false);
            using var streamReader = new StreamReader(stream);
            var streamContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);

            // Assert
            Assert.Equal("logcontent", streamContent);
        }

        [Fact]
        public async Task Test_BlobStorageStreamReader_NotFound()
        {
            // Arrange
            var connnectionString = "UseDevelopmentStorage=true;";
            var marketoplogsArchive = "marketoplogs-archive";

            var storageFactory = new StorageServiceClientFactory(connnectionString);
            var blobStorageStreamReader = new BlobStorageStreamReader(storageFactory, new StorageConfig(marketoplogsArchive));

            await AddBlobToStorage(connnectionString, marketoplogsArchive).ConfigureAwait(false);
            var nameNotExists = "whatthe";

            // Act
            var stream = await blobStorageStreamReader.GetStreamFromStorageAsync(nameNotExists).ConfigureAwait(false);

            // Assert
            Assert.Equal(Stream.Null, stream);
        }

        private static async Task<BlobItemData> AddBlobToStorage(string connectionString, string container)
        {
            var itemToMove = PersistenceTestHelper.CrateRandomBlobItem();
            await using var itemToMoveContentStream = new MemoryStream(Encoding.UTF8.GetBytes(itemToMove.Content));

            var blobServiceClientMarketoplogs = await PersistenceTestHelper.InitTestBlobStorageAsync(connectionString, container).ConfigureAwait(false);
            var containerClient = blobServiceClientMarketoplogs.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(itemToMove.Name);

            // First Upload to storage
            await blobClient.UploadAsync(itemToMoveContentStream).ConfigureAwait(false);

            return itemToMove;
        }
    }
}
