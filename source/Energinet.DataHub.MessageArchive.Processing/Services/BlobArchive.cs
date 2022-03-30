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
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Energinet.DataHub.MessageArchive.Domain.Models;
using Energinet.DataHub.MessageArchive.Domain.Services;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.Processing.Services
{
    public class BlobArchive : IBlobArchive
    {
        private readonly BlobContainerClient _fromContainerClient;
        private readonly BlobContainerClient _toContainerClient;

        public BlobArchive(string connectionString, string fromContainerName, string toContainerName)
        {
            _fromContainerClient = new BlobContainerClient(connectionString, fromContainerName);
            _toContainerClient = new BlobContainerClient(connectionString, toContainerName);
        }

        public async Task<Uri> MoveToArchiveAsync(BlobItemData itemToMove)
        {
            Guard.ThrowIfNull(itemToMove, nameof(itemToMove));

            var fromContainerClient = _fromContainerClient.GetBlockBlobClient(itemToMove.Name);
            var toContainerClient = _toContainerClient.GetBlockBlobClient(itemToMove.Name);

            var options = new BlobCopyFromUriOptions
            {
                Metadata = itemToMove.MetaData,
                Tags = itemToMove.IndexTags,
                AccessTier = AccessTier.Cool,
            };

            var response = await toContainerClient.StartCopyFromUriAsync(itemToMove.Uri, options).ConfigureAwait(false);
            var httpResponse = await response.WaitForCompletionAsync().ConfigureAwait(false);

            if (httpResponse.GetRawResponse().Status != (int)HttpStatusCode.OK)
            {
                throw new InvalidOperationException("MoveToArchiveAsync failed, move and delete not completed with success");
            }

            await fromContainerClient.DeleteAsync().ConfigureAwait(false);
            return toContainerClient.Uri;
        }
    }
}
