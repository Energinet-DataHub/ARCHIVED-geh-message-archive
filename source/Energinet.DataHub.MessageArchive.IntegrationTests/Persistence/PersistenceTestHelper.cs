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
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Persistence
{
    public static class PersistenceTestHelper
    {
        public static async Task<BlobServiceClient> InitTestBlobStorageAsync(string connectionString, string container)
        {
            var storageClient = new BlobServiceClient(connectionString);
            await storageClient
                .GetBlobContainerClient(container)
                .CreateIfNotExistsAsync()
                .ConfigureAwait(false);
            return storageClient;
        }

        public static BlobItemData CrateRandomBlobItem()
        {
            var name = Guid.NewGuid().ToString();

            var metaData = new Dictionary<string, string>() { { "MetaKey1", "MateValue1" }, };
            var indexTags = new Dictionary<string, string>() { { "TagKey1", "TagValue1" }, };
            var content = "logcontent";
            var logUri = new Uri($"http://127.0.0.1:10000/{name}/");
            var item = new BlobItemData(name, metaData, indexTags, content, DateTimeOffset.Now, logUri);
            return item;
        }
    }
}
