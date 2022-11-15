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
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.Models;
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
            ArgumentNullException.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = await base.ParseAsync(blobItemData).ConfigureAwait(false);

            if (blobItemData.ContentStream != null)
            {
                await ParseJsonFromStreamAsync(parsedModel, blobItemData.ContentStream).ConfigureAwait(false);
                parsedModel.CreatedDate ??= parsedModel.LogCreatedDate;
            }

            return parsedModel;
        }

        private static async Task ParseJsonFromStreamAsync(BaseParsedModel parsedModel, Stream contentJsonStream)
        {
            using var sr = new StreamReader(contentJsonStream);
            using var reader = new JsonTextReader(sr);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.Depth == 1
                    && reader.Value != null
                    && (reader.Value as string)!.EndsWith("_MarketDocument", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.RsmName = ReadRsmName((reader.Value as string)!);
                    continue;
                }

                if (reader.Depth == 1
                    && reader.TokenType == JsonToken.PropertyName
                    && reader.Value != null
                    && (reader.Value as string)!.EndsWith("error", StringComparison.OrdinalIgnoreCase))
                {
                    await ReadErrorsAsync(reader, parsedModel).ConfigureAwait(false);
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    switch (reader.Value)
                    {
                        case "mRID":
                            parsedModel.MessageId = await reader.ReadAsStringAsync().ConfigureAwait(false);
                            continue;
                        case "type":
                            parsedModel.MessageType = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "reason.code":
                            parsedModel.ReasonCode = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "process.processType":
                            parsedModel.ProcessType = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "businessSector.type":
                            parsedModel.BusinessSectorType = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;

                        case "sender_MarketParticipant.mRID":
                            parsedModel.SenderGln = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "sender_MarketParticipant.marketRole.type":
                            parsedModel.SenderGlnMarketRoleType = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "receiver_MarketParticipant.mRID":
                            parsedModel.ReceiverGln = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;
                        case "receiver_MarketParticipant.marketRole.type":
                            parsedModel.ReceiverGlnMarketRoleType = await ReadValueFromObjectAsync(reader).ConfigureAwait(false);
                            continue;

                        case "createdDateTime":
                            var createdAsString = await reader.ReadAsStringAsync().ConfigureAwait(false);
                            if (DateTimeOffset.TryParse(createdAsString, out var parsedDateTimeOffSet))
                            {
                                parsedModel.CreatedDate = parsedDateTimeOffSet;
                            }

                            continue;

                        case "MktActivityRecord":
                        case "Series":
                            await ReadTransactionRecordsAsync(reader, parsedModel).ConfigureAwait(false);
                            continue;
                    }
                }
            }
        }

        private static async Task ReadTransactionRecordsAsync(JsonTextReader reader, BaseParsedModel parsedModel)
        {
            var transactionRecords = new List<TransactionRecord>();
            var currentTransactionRecord = new TransactionRecord() { OriginalTransactionIdReferenceId = string.Empty };

            var transactionsCurrentArrayDepth = 0;
            var transactionsCurrentObjectDepth = 0;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartArray:
                        ++transactionsCurrentArrayDepth;
                        continue;

                    case JsonToken.EndArray:
                        --transactionsCurrentArrayDepth;
                        if (transactionsCurrentArrayDepth == 0)
                        {
                            parsedModel.TransactionRecords = transactionRecords;
                            return;
                        }

                        continue;

                    case JsonToken.StartObject:
                        ++transactionsCurrentObjectDepth;
                        continue;
                    case JsonToken.EndObject:
                        --transactionsCurrentObjectDepth;
                        if (transactionsCurrentObjectDepth == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(currentTransactionRecord.MRid))
                            {
                                transactionRecords.Add(new TransactionRecord()
                                {
                                    MRid = currentTransactionRecord.MRid,
                                    OriginalTransactionIdReferenceId = currentTransactionRecord.OriginalTransactionIdReferenceId,
                                });
                            }

                            currentTransactionRecord = new TransactionRecord() { OriginalTransactionIdReferenceId = string.Empty };
                        }

                        continue;

                    case JsonToken.PropertyName when transactionsCurrentObjectDepth == 1:

                        switch (reader.Value)
                        {
                            case "mRID":
                                currentTransactionRecord.MRid = await reader.ReadAsStringAsync().ConfigureAwait(false);
                                continue;
                            case "originalTransactionIDReference_MktActivityRecord.mRID":
                            case "originalTransactionIDReference_Series.mRID":
                                currentTransactionRecord.OriginalTransactionIdReferenceId = await reader.ReadAsStringAsync().ConfigureAwait(false);
                                continue;
                        }

                        continue;

                    default:
                        continue;
                }
            }
        }

        private static async Task ReadErrorsAsync(JsonTextReader reader, BaseParsedModel parsedModel)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.TokenType == JsonToken.PropertyName && (string?)reader.Value == "details")
                {
                    await ReadErrorDetailsAsync(reader, parsedModel).ConfigureAwait(false);
                }
            }
        }

        private static async Task ReadErrorDetailsAsync(JsonTextReader reader, BaseParsedModel parsedModel)
        {
            var errorModels = new List<ParsedErrorModel>();
            var currentErrorCode = string.Empty;
            var currentErrorMessage = string.Empty;
            var transactionsCurrentArrayDepth = 0;
            var transactionsCurrentObjectDepth = 0;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartArray:
                        ++transactionsCurrentArrayDepth;
                        continue;

                    case JsonToken.EndArray:
                        --transactionsCurrentArrayDepth;
                        if (transactionsCurrentArrayDepth == 0)
                        {
                            parsedModel.Errors = errorModels;
                            return;
                        }

                        continue;

                    case JsonToken.StartObject:
                        ++transactionsCurrentObjectDepth;
                        continue;
                    case JsonToken.EndObject:
                        --transactionsCurrentObjectDepth;
                        if (transactionsCurrentObjectDepth == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(currentErrorCode))
                            {
                                errorModels.Add(new ParsedErrorModel(currentErrorCode, currentErrorMessage ?? "No message"));
                            }

                            currentErrorCode = string.Empty;
                            currentErrorMessage = string.Empty;
                        }

                        continue;

                    case JsonToken.PropertyName when transactionsCurrentObjectDepth == 1:

                        switch (reader.Value)
                        {
                            case "code":
                                currentErrorCode = await reader.ReadAsStringAsync().ConfigureAwait(false);
                                continue;
                            case "message":
                                currentErrorMessage = await reader.ReadAsStringAsync().ConfigureAwait(false);
                                continue;
                        }

                        continue;

                    default:
                        continue;
                }
            }
        }

        private static async Task<string> ReadValueFromObjectAsync(JsonTextReader reader)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.Value)
                {
                    case "value":
                        return await reader.ReadAsStringAsync().ConfigureAwait(false) ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string ReadRsmName(string documentName)
        {
            var indexOfSeparator = documentName.IndexOf("_", StringComparison.CurrentCultureIgnoreCase);
            if (indexOfSeparator >= 0)
            {
                var rsmName = documentName[..indexOfSeparator];
#pragma warning disable CA1308
                return rsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }

            return documentName;
        }
    }
}
