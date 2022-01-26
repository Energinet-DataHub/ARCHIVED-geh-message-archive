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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.BlobServices
{
    public class BlobReader : IBlobReader
    {
        private BlobContainerClient _blobContainerClient;

        public BlobReader(
            string connectionString,
            string containerName)
        {
            _blobContainerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<List<BlobItemData>> GetBlobsReadyForProcessingAsync()
        {
            var blobsToProcess = _blobContainerClient.GetBlobsAsync(BlobTraits.All).ConfigureAwait(false);
            var tasks = new List<Task<BlobItemData>>();

            await foreach (var blobItem in blobsToProcess)
            {
                var blobDataTask = DownloadBlobDataAsync(blobItem);
                tasks.Add(blobDataTask);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var downloadedBlobData = tasks.Select(t => t.Result);

            return downloadedBlobData.ToList();
        }

        private async Task<BlobItemData> DownloadBlobDataAsync(BlobItem blobItemToDownload)
        {
            Guard.ThrowIfNull(blobItemToDownload, nameof(blobItemToDownload));

            var metaData = blobItemToDownload.Metadata;
            var indexTags = blobItemToDownload.Tags;
            var properties = blobItemToDownload.Properties;
            var name = blobItemToDownload.Name;

            var blobClient = _blobContainerClient.GetBlobClient(blobItemToDownload.Name);
            var response = await blobClient.DownloadAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(response.Value.Content);
            var content = await streamReader.ReadToEndAsync().ConfigureAwait(false);

            return new BlobItemData(name, metaData, indexTags, content, properties, blobClient.Uri);
        }
    }
}
