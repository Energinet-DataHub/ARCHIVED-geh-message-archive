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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public class LogParserJson : LogParserBlobProperties
    {
        private readonly ILogger<LogParserBlobProperties> _applicationLogging;

        public LogParserJson(ILogger<LogParserBlobProperties> applicationLogging)
        {
            _applicationLogging = applicationLogging;
        }

        public override async Task<BaseParsedModel> ParseAsync(BlobItemData blobItemData)
        {
            Guard.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = await base.ParseAsync(blobItemData).ConfigureAwait(false);

            try
            {
                if (blobItemData.ContentStream != null)
                {
                    await ParseJsonFromStreamAsync(parsedModel, blobItemData.ContentStream).ConfigureAwait(false);
                    parsedModel.CreatedDate ??= parsedModel.LogCreatedDate;
                }
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _applicationLogging.LogError(ex, "Parse Error in LogParserJson, returning base model, name: {Name}", blobItemData.Name);
                parsedModel.ParsingSuccess = false;
            }

            // TODO Parse errors from stream - parsedModel.Errors = JsonErrorParser.ParseErrors(blobItemData.Content);
            return parsedModel;
        }

        private static async Task ParseJsonFromStreamAsync(BaseParsedModel parsedModel, Stream contentJsonStream)
        {
            using var sr = new StreamReader(contentJsonStream);
            using var reader = new JsonTextReader(sr);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.Depth == 1
                    && reader.Path.EndsWith("_MarketDocument", StringComparison.OrdinalIgnoreCase)
                    && reader.TokenType == JsonToken.StartObject)
                {
                    parsedModel.RsmName = ReadRsmName(reader.Path);
                    continue;
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.mRID"))
                    {
                        parsedModel.MessageId = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.type.value"))
                    {
                        parsedModel.MessageType = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['businessSector.type'].value"))
                    {
                        parsedModel.BusinessSectorType = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['reason.code'].value"))
                    {
                        parsedModel.ReasonCode = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.createdDateTime"))
                    {
                        var createdAsString = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        if (DateTimeOffset.TryParse(createdAsString, out var parsedDateTimeOffSet))
                        {
                            parsedModel.CreatedDate = parsedDateTimeOffSet;
                            continue;
                        }
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['process.processType'].value"))
                    {
                        parsedModel.ProcessType = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['receiver_MarketParticipant.mRID'].value"))
                    {
                        parsedModel.ReceiverGln = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['receiver_MarketParticipant.marketRole.type'].value"))
                    {
                        parsedModel.ReceiverGlnMarketRoleType = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['sender_MarketParticipant.mRID'].value"))
                    {
                        parsedModel.SenderGln = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['sender_MarketParticipant.marketRole.type'].value"))
                    {
                        parsedModel.SenderGlnMarketRoleType = await reader.ReadAsStringAsync().ConfigureAwait(false);
                        continue;
                    }
                }

                // MarketDocument
                if (reader.TokenType == JsonToken.StartArray
                    && ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.MktActivityRecord"))
                {
                    await ReadMktActivityRecordsAsync(reader, parsedModel).ConfigureAwait(false);
                }
            }
        }

        private static async Task ReadMktActivityRecordsAsync(JsonTextReader reader, BaseParsedModel parsedModel)
        {
            var transactionRecords = new List<TransactionRecord>();
            var transactionRecordsIndex = 0;
            var currentActivityRecordPath = $"MktActivityRecord[{transactionRecordsIndex}]";
            var currentTransactionRecord = new TransactionRecord();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                Debug.WriteLine($"{reader.Path} - {reader.TokenType}");

                if (reader.TokenType == JsonToken.PropertyName
                    && ExpectedPathEndWithFunc(reader.Path, $"{currentActivityRecordPath}.mRID"))
                {
                    currentTransactionRecord.MRid = await reader.ReadAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (reader.TokenType == JsonToken.PropertyName
                    && ExpectedPathEndWithFunc(reader.Path, $"{currentActivityRecordPath}['originalTransactionIDReference_MktActivityRecord.mRID']"))
                {
                    currentTransactionRecord.OriginalTransactionIdReferenceId = await reader.ReadAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (reader.TokenType == JsonToken.EndObject
                    && ExpectedPathEndWithFunc(reader.Path, $"{currentActivityRecordPath}"))
                {
                    if (!string.IsNullOrWhiteSpace(currentTransactionRecord.MRid))
                    {
                        transactionRecords.Add(new TransactionRecord()
                        {
                            MRid = currentTransactionRecord.MRid,
                            OriginalTransactionIdReferenceId = currentTransactionRecord.OriginalTransactionIdReferenceId,
                        });
                    }

                    currentActivityRecordPath = $"MktActivityRecord[{++transactionRecordsIndex}]";
                    currentTransactionRecord = new TransactionRecord() { OriginalTransactionIdReferenceId = string.Empty };
                    continue;
                }

                if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.MktActivityRecord")
                    && reader.TokenType == JsonToken.EndArray)
                {
                    Debug.WriteLine($"End _MarketDocument.MktActivityRecord");
                    parsedModel.TransactionRecords = transactionRecords;
                    break;
                }
            }
        }

        private static bool ExpectedPathEndWithFunc(string currentPath, string endsWith) => currentPath.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase);

        private static string ReadRsmName(string documentName)
        {
            var indexOfSeparator = documentName.IndexOf("_", StringComparison.CurrentCultureIgnoreCase);
            if (indexOfSeparator >= 0)
            {
                var rsmName = documentName.Substring(0, indexOfSeparator);
#pragma warning disable CA1308
                return rsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }

            return documentName;
        }
    }
}
