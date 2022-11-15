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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Processing.Services;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Persistence.Services
{
    public class BlobReader : IBlobReader
    {
        private readonly ILogger<BlobReader> _logger;
        private readonly BlobContainerClient _blobContainerClient;

        public BlobReader(
            string connectionString,
            string containerName,
            ILogger<BlobReader> logger)
        {
            _logger = logger;
            _blobContainerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<List<BlobItemData>> GetBlobsReadyForProcessingAsync()
        {
            var blobPagesToProcess = _blobContainerClient
                .GetBlobsAsync(BlobTraits.All)
                .AsPages(default, 500);

            var tasks = new List<Task<BlobItemData>>();

            await foreach (Azure.Page<BlobItem> blobPage in blobPagesToProcess)
            {
                foreach (var blobItem in blobPage.Values)
                {
                    var blobDataTask = DownloadBlobDataAsync(blobItem);
                    tasks.Add(blobDataTask);
                }

                if (tasks.Count > 0 || string.IsNullOrEmpty(blobPage.ContinuationToken))
                {
                    break;
                }
            }

            _logger.LogInformation("Starts downloading log content for {TaskCount} tasks", tasks.Count);

            await Task.WhenAll(tasks).ConfigureAwait(false);

            _logger.LogInformation("Downloading done for all {TaskCount} tasks", tasks.Count);

            var downloadedBlobData = tasks.Select(t => t.Result);

            return downloadedBlobData.ToList();
        }

        private async Task<BlobItemData> DownloadBlobDataAsync(BlobItem blobItemToDownload)
        {
            ArgumentNullException.ThrowIfNull(blobItemToDownload, nameof(blobItemToDownload));

            var metaData = blobItemToDownload.Metadata ?? new Dictionary<string, string>();
            var indexTags = blobItemToDownload.Tags ?? new Dictionary<string, string>();
            var properties = blobItemToDownload.Properties;
            var name = blobItemToDownload.Name;

            var blobClient = _blobContainerClient.GetBlobClient(blobItemToDownload.Name);
            var createdOnUtc = properties.CreatedOn.GetValueOrDefault().ToUniversalTime();

            var response = await blobClient.DownloadStreamingAsync().ConfigureAwait(false);
            var blobItemDataJson = new BlobItemData(name, metaData, indexTags, createdOnUtc, blobClient.Uri);
            blobItemDataJson.ContentStream = response.Value.Content;

            return blobItemDataJson;
        }
    }
}
