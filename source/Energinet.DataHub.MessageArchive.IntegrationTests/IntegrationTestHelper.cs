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
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace Energinet.DataHub.MessageArchive.IntegrationTests
{
    public static class IntegrationTestHelper
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
            var content = GenerateStreamFromString("logcontent");
            var logUri = new Uri($"http://127.0.0.1:10000/{name}/");
            var item = new BlobItemData(name, metaData, indexTags, DateTimeOffset.Now, logUri)
            {
                ContentStream = content,
                ContentLength = content?.Length ?? 0,
            };
            return item;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
#pragma warning disable CA2000
            var writer = new StreamWriter(stream);
#pragma warning restore CA2000
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
