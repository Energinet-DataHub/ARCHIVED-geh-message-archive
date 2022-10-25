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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.LogParsers
{
    [UnitTest]
    public class LogParserJsonTests
    {
        [Fact]
        public async Task Parse_JSON_Stream_RequestChangeOfSupplier()
        {
            // Arrange
            var filePathWithName = "assets/requestchangeofsupplier.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);
            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("23984918", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal(string.Empty, parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-09-07T09:30:47Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E65", parsedModel.ProcessType);
            Assert.Equal("DDZ", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.ReceiverGln);
            Assert.Equal("DDQ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5178861303303", parsedModel.SenderGln);
            Assert.Equal("392", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "24770489" && string.IsNullOrEmpty(r.OriginalTransactionIdReferenceId));
        }

        [Fact]
        public async Task Parse_JSON_Stream_ConfirmRequestChangeOfSupplier()
        {
            // Arrange
            var filePathWithName = "assets/confirmrequestchangeofsupplier.json";
            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("c62ff0ac-e56e-4b26-90bd-e96faa5a4089", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal("A01", parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-09-05T12:54:57Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E65", parsedModel.ProcessType);
            Assert.Equal("DDQ", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5178861303303", parsedModel.ReceiverGln);
            Assert.Equal("DDZ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.SenderGln);
            Assert.Equal("414", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "b860ada2-16e5-4942-a513-3dbd414fc344" && r.OriginalTransactionIdReferenceId == "12460889");
        }

        [Fact]
        public async Task Parse_JSON_Stream_RejectRequestChangeOfSupplier()
        {
            // Arrange
            var filePathWithName = "assets/rejectrequestchangeofsupplier.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("551e7c64-97b4-4ef6-aaf7-e0b990f2b197", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal("A02", parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-09-05T13:01:05Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E65", parsedModel.ProcessType);
            Assert.Equal("DDQ", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5178861303303", parsedModel.ReceiverGln);
            Assert.Equal("DDZ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.SenderGln);
            Assert.Equal("414", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "e95d96a1-6c8c-4dfc-9e2b-9cbdbbd47dea" && r.OriginalTransactionIdReferenceId == "14760489");
        }

        [Fact]
        public async Task Parse_JSON_Stream_ConfirmRequestChangeOfSupplier_MultiActivityRecords()
        {
            // Arrange
            var filePathWithName = "assets/multiactivityrecords_confirmrequestchangeofsupplier.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("c62ff0ac-e56e-4b26-90bd-e96faa5a4089", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal("A01", parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-09-05T12:54:57Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E65", parsedModel.ProcessType);
            Assert.Equal("DDQ", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5178861303303", parsedModel.ReceiverGln);
            Assert.Equal("DDZ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.SenderGln);
            Assert.Equal("414", parsedModel.MessageType);
            Assert.Equal(3, parsedModel.TransactionRecords.Count());
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "b860ada2-16e5-4942-a513-3dbd414fc344" && r.OriginalTransactionIdReferenceId == "12460889");
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "b860ada2-16e5-4942-a513-3dbd414fc343" && r.OriginalTransactionIdReferenceId == "12460888");
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "b860ada2-16e5-4942-a513-3dbd414fc342" && r.OriginalTransactionIdReferenceId == "12460887");
        }

        [Fact]
        public async Task Parse_JSON_Stream_RequestValidatedMeasureData()
        {
            // Arrange
            var filePathWithName = "assets/requestValidatedMeasureData.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("12345687", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal(string.Empty, parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-12-17T09:30:47Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E23", parsedModel.ProcessType);
            Assert.Equal("DGL", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.ReceiverGln);
            Assert.Equal("DDQ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5799999933318", parsedModel.SenderGln);
            Assert.Equal("E73", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "1568914" && string.IsNullOrEmpty(r.OriginalTransactionIdReferenceId));
        }

        [Fact]
        public async Task Parse_JSON_Stream_NotifyValidatedMeasureData()
        {
            // Arrange
            var filePathWithName = "assets/notifyValidatedMeasureData.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("C1876453", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal(string.Empty, parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2022-12-17T09:30:47Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E23", parsedModel.ProcessType);
            Assert.Equal("DGL", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.ReceiverGln);
            Assert.Equal("MDR", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5799999933317", parsedModel.SenderGln);
            Assert.Equal("E66", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "C1876456" && r.OriginalTransactionIdReferenceId == "C1875000");
        }

        [Fact]
        public async Task Parse_JSON_Stream_RejectRequestValidatedMeasureData()
        {
            // Arrange
            var filePathWithName = "assets/rejectRequestValidatedMeasureData.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.TransactionRecords);
            Assert.Equal("25869814", parsedModel.MessageId);
            Assert.Equal("23", parsedModel.BusinessSectorType);
            Assert.Equal("A02", parsedModel.ReasonCode);
            Assert.Equal(DateTimeOffset.Parse("2001-12-17T09:30:47Z", CultureInfo.InvariantCulture), parsedModel.CreatedDate);
            Assert.Equal("E23", parsedModel.ProcessType);
            Assert.Equal("DDQ", parsedModel.ReceiverGlnMarketRoleType);
            Assert.Equal("5799999933318", parsedModel.ReceiverGln);
            Assert.Equal("DDZ", parsedModel.SenderGlnMarketRoleType);
            Assert.Equal("5790001330552", parsedModel.SenderGln);
            Assert.Equal("ERR", parsedModel.MessageType);
            Assert.Single(parsedModel.TransactionRecords);
            Assert.Contains(parsedModel.TransactionRecords, r => r.MRid == "25869147" && r.OriginalTransactionIdReferenceId == "25836914");
        }

        [Fact]
        public async Task Parse_JSON_Stream_ValidationError_Parsing()
        {
            // Arrange
            var filePathWithName = "assets/validation_exception.json";

            using var fileStream = File.Open(filePathWithName, FileMode.Open);

            var jsonStreamParser = new LogParserJson(new Mock<ILogger<LogParserBlobProperties>>().Object);
            var blobItem = MockedTypes.BlobItemDataStream("json", fileStream);

            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);

            Assert.NotNull(parsedModel);
            Assert.NotNull(parsedModel.Errors);
            Assert.Contains(parsedModel.Errors, r => r.Code == "invalid_UUID" && r.Message == "Bundle Id must have a valid guid.");
            Assert.Contains(parsedModel.Errors, r => r.Code == "invalid_UUID2" && r.Message == "Bundle Id must have a valid guid.2");
            Assert.Equal(2, parsedModel.Errors.Count());
        }

        private static BaseParsedModel ParseJsonFromFileStream(string filePathWithName)
        {
            var parsedModel = new BaseParsedModel()
            {
                ReasonCode = string.Empty,
                BusinessSectorType = string.Empty,
                ProcessType = string.Empty,
            };

            using var s = File.Open(filePathWithName, FileMode.Open);
            using var sr = new StreamReader(s);
            using var reader = new JsonTextReader(sr);

            while (reader.Read())
            {
                Debug.WriteLine($"{reader.Path} - {reader.TokenType}");

                if (reader.Depth == 1
                    && reader.Path.EndsWith("_MarketDocument", StringComparison.OrdinalIgnoreCase)
                    && reader.TokenType == JsonToken.StartObject)
                {
                    parsedModel.RsmName = ReadRsmName(reader.Path);
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.mRID"))
                    {
                        parsedModel.MessageId = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.type.value"))
                    {
                        parsedModel.MessageType = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['businessSector.type'].value"))
                    {
                        parsedModel.BusinessSectorType = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['reason.code'].value"))
                    {
                        parsedModel.ReasonCode = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.createdDateTime"))
                    {
                        parsedModel.CreatedDate = reader.ReadAsDateTimeOffset();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['process.processType'].value"))
                    {
                        parsedModel.ProcessType = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['receiver_MarketParticipant.mRID'].value"))
                    {
                        parsedModel.ReceiverGln = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['receiver_MarketParticipant.marketRole.type'].value"))
                    {
                        parsedModel.ReceiverGlnMarketRoleType = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['sender_MarketParticipant.mRID'].value"))
                    {
                        parsedModel.SenderGln = reader.ReadAsString();
                        continue;
                    }

                    if (ExpectedPathEndWithFunc(reader.Path, "_MarketDocument['sender_MarketParticipant.marketRole.type'].value"))
                    {
                        parsedModel.SenderGlnMarketRoleType = reader.ReadAsString();
                        continue;
                    }
                }

                // MarketDocument
                if (reader.TokenType == JsonToken.StartArray
                    && ExpectedPathEndWithFunc(reader.Path, "_MarketDocument.MktActivityRecord"))
                {
                    ReadMktActivityRecords(reader, parsedModel);
                }
            }

            return parsedModel;
        }

        private static void ReadMktActivityRecords(JsonTextReader reader, BaseParsedModel parsedModel)
        {
            Debug.WriteLine($"Start _MarketDocument.MktActivityRecord");

            var transactionRecords = new List<TransactionRecord>();
            var transactionRecordsIndex = 0;
            var currentActivityRecordPath = $"MktActivityRecord[{transactionRecordsIndex}]";
            var currentTransactionRecord = new TransactionRecord();

            while (reader.Read())
            {
                Debug.WriteLine($"{reader.Path} - {reader.TokenType}");

                if (reader.TokenType == JsonToken.PropertyName
                    && ExpectedPathEndWithFunc(reader.Path, $"{currentActivityRecordPath}.mRID"))
                {
                    currentTransactionRecord.MRid = reader.ReadAsString();
                    continue;
                }

                if (reader.TokenType == JsonToken.PropertyName
                    && ExpectedPathEndWithFunc(reader.Path, $"{currentActivityRecordPath}['originalTransactionIDReference_MktActivityRecord.mRID']"))
                {
                    currentTransactionRecord.OriginalTransactionIdReferenceId = reader.ReadAsString();
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
