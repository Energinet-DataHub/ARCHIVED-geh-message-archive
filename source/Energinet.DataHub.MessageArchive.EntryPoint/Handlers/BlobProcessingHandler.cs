﻿// Copyright 2020 Energinet DataHub A/S
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
        private readonly IStorageWriter<BaseParsedModel> _storageWriter;
        private readonly ILogger<BlobProcessingHandler> _logger;

        public BlobProcessingHandler(
            IBlobReader blobReader,
            IStorageWriter<BaseParsedModel> storageWriter,
            ILogger<BlobProcessingHandler> logger)
        {
            _blobReader = blobReader;
            _storageWriter = storageWriter;
            _logger = logger;
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
                    await _storageWriter.WriteAsync(parsedModel).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Could not find parsed for log: {name}", blobItemData.Name);
                }
            }
        }
    }
}
