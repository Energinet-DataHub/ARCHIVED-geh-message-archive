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
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.BlobServices;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Handlers
{
    public class BlobProcessingHandler : IBlobProcessingHandler
    {
        private readonly IBlobReader _blobReader;

        public BlobProcessingHandler(IBlobReader blobReader)
        {
            _blobReader = blobReader;
        }

        public async Task HandleAsync()
        {
            var blobDataToProcess = await _blobReader.GetBlobsReadyForProcessingAsync().ConfigureAwait(false);

            foreach (var blobItemData in blobDataToProcess)
            {
                var contentType = blobItemData.MetaData.TryGetValue("contenttype", out var contentTypeValue) ? contentTypeValue : string.Empty;

                var parser = ParserFinder.FindParser(contentType, blobItemData.Content);
                if (parser is { })
                {
                    var parsedModel = parser.Parse(blobItemData);
                }
                else
                {
                    // ON ERROR
                }
            }
        }
    }
}
