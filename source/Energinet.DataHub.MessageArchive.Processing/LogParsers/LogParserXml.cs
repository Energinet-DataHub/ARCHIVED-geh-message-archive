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
using System.Xml;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public class LogParserXml : LogParserBlobProperties
    {
        private const string RsmNameSeparator = "_";
        private static XmlReaderSettings? _xmlReaderSettings;
        private readonly ILogger<LogParserBlobProperties> _applicationLogging;

        public LogParserXml(ILogger<LogParserBlobProperties> applicationLogging)
        {
            _applicationLogging = applicationLogging;
            _xmlReaderSettings = new XmlReaderSettings()
            {
                Async = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
            };
        }

        public override async Task<BaseParsedModel> ParseAsync(BlobItemData blobItemData)
        {
            ArgumentNullException.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = await base.ParseAsync(blobItemData).ConfigureAwait(false);

            if (blobItemData.ContentLength > 0)
            {
                await ParseCimXmlFromStreamAsync(parsedModel, blobItemData.ContentStream).ConfigureAwait(false);
                parsedModel.CreatedDate ??= parsedModel.LogCreatedDate;
            }

            return parsedModel;
        }

        private static async Task ParseCimXmlFromStreamAsync(BaseParsedModel parsedModel, Stream contentStream)
        {
            using var xmlReader = XmlReader.Create(contentStream, _xmlReaderSettings);

            var tempTransactionRecords = new List<TransactionRecord>();

            var continueRead = await xmlReader.ReadAsync().ConfigureAwait(false);

            do
            {
                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.EndsWith("_MarketDocument", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.RsmName = ReadRsmName(xmlReader.LocalName);
                    continueRead = await xmlReader.ReadAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.MRid, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.MessageId = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.Type, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.MessageType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.ProcessProcessType, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.ProcessType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.BusinessSectorType, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.BusinessSectorType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.ReasonCode, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.ReasonCode = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.CreatedDateTime, StringComparison.OrdinalIgnoreCase))
                {
                    var dateString = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    parsedModel.CreatedDate = DateTimeOffset.TryParse(dateString, out var result) ? result : null;
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.SenderMarketParticipantmRid, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.SenderGln = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.SenderMarketParticipantmarketRoletype, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.SenderGlnMarketRoleType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.ReceiverMarketParticipantmRid, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.ReceiverGln = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.ReceiverMarketParticipantmarketRoletype, StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.ReceiverGlnMarketRoleType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element &&
                    (xmlReader.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase) ||
                     xmlReader.LocalName.Equals("Series", StringComparison.OrdinalIgnoreCase)))
                {
                    var transactionRecord = await ReadTransactionRecordsAsync(xmlReader).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(transactionRecord.MRid))
                    {
                        tempTransactionRecords.Add(transactionRecord);
                    }
                }

                continueRead = await xmlReader.ReadAsync().ConfigureAwait(false);
            }
            while (continueRead);

            if (tempTransactionRecords.Count > 0)
            {
                parsedModel.TransactionRecords = tempTransactionRecords;
            }
        }

        private static async Task<TransactionRecord> ReadTransactionRecordsAsync(XmlReader xmlReader)
        {
            var payloadElementName = xmlReader.LocalName;
            var transactionRecord = new TransactionRecord { OriginalTransactionIdReferenceId = string.Empty };
            var readPayload = true;

            while (readPayload)
            {
                if (xmlReader.Depth > 2)
                {
                    readPayload = await xmlReader.ReadAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.MRid, StringComparison.OrdinalIgnoreCase))
                {
                    transactionRecord.MRid = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid, StringComparison.OrdinalIgnoreCase))
                {
                    transactionRecord.OriginalTransactionIdReferenceId = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.OriginalTransactionIdReferenceSeriesmRid, StringComparison.OrdinalIgnoreCase))
                {
                    transactionRecord.OriginalTransactionIdReferenceId = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals(ElementNames.TransactionReason, StringComparison.OrdinalIgnoreCase))
                {
                    (string Code, string Text) reason = await ReadTransactionReasonAsync(xmlReader).ConfigureAwait(false);
                    transactionRecord.ReasonCode = reason.Code;
                    transactionRecord.ReasonText = reason.Text;
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.EndElement
                    && xmlReader.LocalName.Equals(payloadElementName, StringComparison.OrdinalIgnoreCase))
                {
                    return transactionRecord;
                }

                readPayload = await xmlReader.ReadAsync().ConfigureAwait(false);
            }

            return transactionRecord;
        }

        private static async Task<(string Code, string Message)> ReadTransactionReasonAsync(XmlReader xmlReader)
        {
            var payloadElementName = xmlReader.LocalName;
            var code = string.Empty;
            var text = string.Empty;
            var readReason = true;

            while (readReason)
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element when xmlReader.LocalName.Equals("Code", StringComparison.OrdinalIgnoreCase):
                        code = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        continue;
                    case XmlNodeType.Element when xmlReader.LocalName.Equals("text", StringComparison.OrdinalIgnoreCase):
                        text = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        continue;
                    case XmlNodeType.EndElement when xmlReader.LocalName.Equals(payloadElementName, StringComparison.OrdinalIgnoreCase):
                        return (code, text);
                    default:
                        readReason = await xmlReader.ReadAsync().ConfigureAwait(false);
                        break;
                }
            }

            return (string.Empty, string.Empty);
        }

        private static string ReadRsmName(string rootName)
        {
            var indexOfSeparator = rootName.IndexOf(RsmNameSeparator, StringComparison.CurrentCultureIgnoreCase);
            if (indexOfSeparator >= 0)
            {
                var rsmName = rootName[..indexOfSeparator];
#pragma warning disable CA1308
                return rsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }

            return rootName;
        }
    }
}
