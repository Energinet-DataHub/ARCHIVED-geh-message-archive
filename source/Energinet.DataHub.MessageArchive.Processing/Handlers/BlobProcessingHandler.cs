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
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Energinet.DataHub.MessageArchive.Processing.Mappers;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Processing.Services;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Processing.Handlers
{
    public class BlobProcessingHandler : IBlobProcessingHandler
    {
        private readonly IBlobReader _blobReader;
        private readonly IBlobArchive _blobArchive;
        private readonly IStorageWriter<CosmosRequestResponseLog> _storageWriter;
        private readonly ILogger<BlobProcessingHandler> _processingLogger;
        private readonly ILogger<LogParserBlobProperties> _parserLogger;

        public BlobProcessingHandler(
            IBlobReader blobReader,
            IBlobArchive blobArchive,
            IStorageWriter<CosmosRequestResponseLog> storageWriter,
            ILogger<BlobProcessingHandler> processingLogger,
            ILogger<LogParserBlobProperties> parserLogger)
        {
            _blobReader = blobReader;
            _blobArchive = blobArchive;
            _storageWriter = storageWriter;
            _processingLogger = processingLogger;
            _parserLogger = parserLogger;
        }

        public async Task HandleAsync()
        {
            var blobDataToProcess = await _blobReader.GetBlobsReadyForProcessingAsync().ConfigureAwait(false);

            foreach (var blobItemData in blobDataToProcess)
            {
                var parser = ParserFinder.FindParser(blobItemData, _parserLogger);

                try
                {
                    await ParseAndSaveAsync(parser, blobItemData).ConfigureAwait(false);
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _parserLogger.LogError(ex, "Parse Error in parser {ParserName}, returning base model, item name: {Name}", nameof(parser), blobItemData.Name);
                    await ParseWithBaseParserAndSetParsingFailedAndSaveAsync(blobItemData).ConfigureAwait(false);
                }
            }
        }

        private async Task ParseAndSaveAsync(ILogParser parser, BlobItemData blobItemData)
        {
            _parserLogger.LogInformation("Starts parsing with parser {ParserName} item name: {Name}, ContentLength: {ContentLength}", parser.GetType().Name, blobItemData.Name, blobItemData.ContentLength);

            var parsedModel = await parser.ParseAsync(blobItemData).ConfigureAwait(false);

            _parserLogger.LogInformation("Parse completed with expected success");

            await SaveToStorageAndMoveToArchiveAsync(parsedModel, blobItemData).ConfigureAwait(false);
        }

        private async Task ParseWithBaseParserAndSetParsingFailedAndSaveAsync(BlobItemData blobItemData)
        {
            var baseParser = new LogParserBlobProperties();
            var parsedModel = await baseParser.ParseAsync(blobItemData).ConfigureAwait(false);
            parsedModel.ParsingSuccess = false;
            await SaveToStorageAndMoveToArchiveAsync(parsedModel, blobItemData).ConfigureAwait(false);
        }

        private async Task SaveToStorageAndMoveToArchiveAsync(BaseParsedModel parsedModel, BlobItemData blobItemData)
        {
            var archiveUri = await _blobArchive.MoveToArchiveAsync(blobItemData).ConfigureAwait(false);

            var cosmosModel = CosmosRequestResponseLogMapper.ToCosmosRequestResponseLog(parsedModel);
            cosmosModel.BlobContentUri = archiveUri.AbsoluteUri;
            await _storageWriter.WriteAsync(cosmosModel).ConfigureAwait(false);
        }
    }
}
