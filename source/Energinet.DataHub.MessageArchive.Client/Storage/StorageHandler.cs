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
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Energinet.DataHub.MessageArchive.Client.Abstractions.Storage;
using Energinet.DataHub.MessageArchive.Client.Helpers;

namespace Energinet.DataHub.MessageArchive.Client.Storage
{
        public sealed class StorageHandler : IStorageHandler
    {
        private readonly IStorageServiceClientFactory _storageServiceClientFactory;
        private readonly StorageConfig _storageConfig;

        public StorageHandler(
            IStorageServiceClientFactory storageServiceClientFactory,
            StorageConfig storageConfig)
        {
            _storageServiceClientFactory = storageServiceClientFactory;
            _storageConfig = storageConfig;
        }

        public async Task<Stream> GetStreamFromStorageAsync(Uri contentPath)
        {
            if (contentPath is null) throw new ArgumentNullException(nameof(contentPath));

            try
            {
                var blobName = UriHelper.DecodeBlobName(contentPath, _storageConfig.AzureBlobStorageContainerName);
                var blobClient = CreateBlobClient(blobName);
                var response = await blobClient
                        .DownloadStreamingAsync()
                        .ConfigureAwait(false);

                return response.Value.Content;
            }
            catch (RequestFailedException e)
            {
                throw new RequestFailedException("Error downloading file from storage", e);
            }
        }

        private BlobClient CreateBlobClient(string blobFileName)
        {
            return _storageServiceClientFactory
                .Create()
                .GetBlobContainerClient(_storageConfig.AzureBlobStorageContainerName)
                .GetBlobClient(blobFileName);
        }
    }
}
