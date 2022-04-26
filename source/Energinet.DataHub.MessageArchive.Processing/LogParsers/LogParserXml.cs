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
using System.Xml.Linq;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public class LogParserXml : LogParserBlobProperties
    {
        private const string RsmNameSeparator = "_";
        private readonly ILogger<LogParserBlobProperties> _applicationLogging;

        public LogParserXml(ILogger<LogParserBlobProperties> applicationLogging)
        {
            _applicationLogging = applicationLogging;
        }

        public override BaseParsedModel Parse(BlobItemData blobItemData)
        {
            Guard.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = base.Parse(blobItemData);

            try
            {
                var xmlDocument = XElement.Parse(blobItemData.Content);
                XNamespace ns = xmlDocument.Name.Namespace;

                var mridValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.MRid}");
                var typeValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.Type}");
                var processTypeValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ProcessProcessType}");
                var businessSectorTypeValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.BusinessSectorType}");
                var reasonCodeTypeValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ReasonCode}");
                var senderGlnValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.SenderMarketParticipantmRid}");
                var senderMarketRoleValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.SenderMarketParticipantmarketRoletype}");
                var receiverGlnValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ReceiverMarketParticipantmRid}");
                var receiverMarketRoleValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ReceiverMarketParticipantmarketRoletype}");
                var createdDataValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.CreatedDateTime}");

                var rsmName = ReadRsmName(xmlDocument);
                var activityRecords = ParseTransactionRecords(xmlDocument, ns);

                parsedModel.MessageId = mridValue;
                parsedModel.MessageType = typeValue;
                parsedModel.ProcessType = processTypeValue;
                parsedModel.BusinessSectorType = businessSectorTypeValue;
                parsedModel.ReasonCode = reasonCodeTypeValue;
                parsedModel.SenderGln = senderGlnValue;
                parsedModel.SenderGlnMarketRoleType = senderMarketRoleValue;
                parsedModel.ReceiverGln = receiverGlnValue;
                parsedModel.ReceiverGlnMarketRoleType = receiverMarketRoleValue;
                parsedModel.TransactionRecords = activityRecords;
                parsedModel.RsmName = rsmName;

                var createdDateParsed = DateTimeOffset.TryParse(createdDataValue, out var createdDataValueParsed);
                parsedModel.CreatedDate = createdDateParsed ? createdDataValueParsed : parsedModel.LogCreatedDate;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _applicationLogging.LogError(ex, "Parse Error in LogParserXml, returning base model");
                parsedModel.ParsingSuccess = false;
            }

            return parsedModel;
        }

        private static string ReadValueOrEmptyString(XElement xmlDocument, string name)
        {
            var node = xmlDocument.Elements(name).FirstOrDefault();
            var value = node?.Value ?? string.Empty;
            return value;
        }

        private static IEnumerable<TransactionRecord> ParseTransactionRecords(XElement xmlDocument, XNamespace ns)
        {
            var mktActivityRecords = xmlDocument
                .Elements($"{ns + "MktActivityRecord"}")
                .ToList();

            if (mktActivityRecords.Any())
            {
                return ReadTransactionRecords(
                    mktActivityRecords,
                    ns,
                    ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid);
            }

            var seriesRecords = xmlDocument
                .Elements($"{ns + "Series"}")
                .ToList();

            if (seriesRecords.Any())
            {
                return ReadTransactionRecords(
                    seriesRecords,
                    ns,
                    ElementNames.OriginalTransactionIdReferenceSeriesmRid);
            }

            return Array.Empty<TransactionRecord>();
        }

        private static IEnumerable<TransactionRecord> ReadTransactionRecords(
            IEnumerable<XElement> transactionRecords,
            XNamespace ns,
            string transactionReferenceIdName)
        {
            var parsedTransactionRecords = new List<TransactionRecord>();

            foreach (var record in transactionRecords)
            {
                var mridValue = ReadValueOrEmptyString(record, $"{ns + ElementNames.MRid}");
                var originalTransactionReferenceId = ReadValueOrEmptyString(record, $"{ns + transactionReferenceIdName}");

                if (!string.IsNullOrWhiteSpace(mridValue))
                {
                    parsedTransactionRecords.Add(new TransactionRecord
                    {
                        MRid = mridValue,
                        OriginalTransactionIdReferenceId = originalTransactionReferenceId,
                    });
                }
            }

            return parsedTransactionRecords;
        }

        private static string ReadRsmName(XElement xmlDocument)
        {
            var rootName = xmlDocument.Name.LocalName;
            var indexOfSeparator = rootName.IndexOf(RsmNameSeparator, StringComparison.CurrentCultureIgnoreCase);
            if (indexOfSeparator >= 0)
            {
                var rsmName = rootName.Substring(0, indexOfSeparator);
#pragma warning disable CA1308
                return rsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }

            return rootName;
        }
    }
}
