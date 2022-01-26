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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.BlobServices;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Storage;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Handlers
{
    public class BlobProcessingHandler : IBlobProcessingHandler
    {
        private readonly IBlobReader _blobReader;
        private readonly IStorageWriter<CosmosRequestResponseLog> _storageWriter;
        private readonly ILogger<BlobProcessingHandler> _logger;

        public BlobProcessingHandler(
            IBlobReader blobReader,
            IStorageWriter<CosmosRequestResponseLog> storageWriter,
            ILogger<BlobProcessingHandler> logger)
        {
            _blobReader = blobReader;
            _storageWriter = storageWriter;
            _logger = logger;
        }

        public async Task HandleAsync()
        {
            var blobDataToProcess = await _blobReader.GetBlobsReadyForProcessingAsync().ConfigureAwait(false);
            var orderedEnumerable = blobDataToProcess.OrderBy(e =>
                e.MetaData.TryGetValue("httpdatatype", out var httpdata) && httpdata.Equals("request"));

            // Take requests first .
            // find responess where invacation id can be found in requests.
            // Handle Error XML, JSON and so on
            // Move or mark blob
            foreach (var blobItemData in blobDataToProcess)
            {
                var contentType = blobItemData.MetaData.TryGetValue("contenttype", out var contentTypeValue) ? contentTypeValue : string.Empty;

                var parser = ParserFinder.FindParser(contentType, blobItemData.Content);
                if (parser is { })
                {
                    var parsedModel = parser.Parse(blobItemData);
                    var cosmosModel = Mappers.CosmosRequestResponseLogMapper.ToCosmosRequestResponseLog(parsedModel);
                    await _storageWriter.WriteAsync(cosmosModel).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Could not find parsed for log: {name}", blobItemData.Name);
                }
            }
        }
    }
}
