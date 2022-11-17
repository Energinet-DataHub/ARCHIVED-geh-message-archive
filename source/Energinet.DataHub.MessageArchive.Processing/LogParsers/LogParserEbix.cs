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
// limitations under the License.using System;

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
    public class LogParserEbix : LogParserBlobProperties
    {
        private static XmlReaderSettings? _xmlReaderSettings;
        private readonly ILogger<LogParserBlobProperties> _applicationLogging;

        public LogParserEbix(ILogger<LogParserBlobProperties> applicationLogging)
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
                await ParseEbixFromStreamAsync(parsedModel, blobItemData.ContentStream).ConfigureAwait(false);
                parsedModel.CreatedDate ??= parsedModel.LogCreatedDate;
            }

            return parsedModel;
        }

        private static async Task ParseEbixFromStreamAsync(BaseParsedModel parsedModel, Stream contentStream)
        {
            using var xmlReader = XmlReader.Create(contentStream, _xmlReaderSettings);

            var tempTransactionRecords = new List<TransactionRecordEbix>();

            while (await xmlReader.ReadAsync().ConfigureAwait(false))
            {
                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.StartsWith("DK_", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModel.RsmName = ReadRsmName(xmlReader.LocalName);
                    continue;
                }

                if (xmlReader.LocalName.Equals("HeaderEnergyDocument", StringComparison.OrdinalIgnoreCase))
                {
                    var continueRead = true;

                    do
                    {
                        if (xmlReader.IsElement("Identification"))
                        {
                            parsedModel.MessageId = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            continue;
                        }

                        if (xmlReader.IsElement("DocumentType"))
                        {
                            parsedModel.MessageType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            continue;
                        }

                        if (xmlReader.IsElement("Creation"))
                        {
                            var dateString = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            parsedModel.CreatedDate = DateTimeOffset.TryParse(dateString, out var result) ? result : null;
                            continue;
                        }

                        if (xmlReader.IsElement("SenderEnergyParty"))
                        {
                            var senderResult = await ReadSenderEnergyPartyAsync(xmlReader).ConfigureAwait(false);
                            parsedModel.SenderGln = senderResult.Identification;
                            parsedModel.SenderGlnMarketRoleType = senderResult.Role;
                            continue;
                        }

                        if (xmlReader.IsElement("RecipientEnergyParty"))
                        {
                            var recipientResult = await ReadRecipientEnergyPartyAsync(xmlReader).ConfigureAwait(false);
                            parsedModel.ReceiverGln = recipientResult.Identification;
                            parsedModel.ReceiverGlnMarketRoleType = recipientResult.Role;
                            continue;
                        }

                        if (xmlReader.IsEndElement("HeaderEnergyDocument"))
                        {
                            break;
                        }

                        continueRead = await xmlReader.ReadAsync().ConfigureAwait(false);
                    }
                    while (continueRead);
                }

                if (xmlReader.IsElement("ProcessEnergyContext"))
                {
                    var energyContext = await ReadProcessEnergyContextAsync(xmlReader).ConfigureAwait(false);
                    parsedModel.ProcessType = energyContext.ProcessType;
                    parsedModel.BusinessSectorType = energyContext.BusinessSectorType;
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.StartsWith("Payload", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionRecord = await ReadPayloadAsync(xmlReader).ConfigureAwait(false);
                    tempTransactionRecords.Add(transactionRecord);
                }
            }

            if (tempTransactionRecords.Count > 0)
            {
                parsedModel.TransactionRecords = tempTransactionRecords;
                parsedModel.ReasonCode = tempTransactionRecords[0].StatusType ?? string.Empty;
            }
        }

        private static async Task<TransactionRecordEbix> ReadPayloadAsync(XmlReader xmlReader)
        {
            var payloadElementName = xmlReader.LocalName;
            var transactionRecord = new TransactionRecordEbix() { OriginalTransactionIdReferenceId = string.Empty, StatusType = string.Empty };
            var readPayload = true;

            while (readPayload)
            {
                if (xmlReader.Depth > 2)
                {
                    readPayload = await xmlReader.ReadAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsElement("Identification"))
                {
                    transactionRecord.MRid = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsElement("StatusType"))
                {
                    transactionRecord.StatusType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsElement("OriginalBusinessDocumentReferenceIdentity"))
                {
                    transactionRecord.OriginalTransactionIdReferenceId = await ReadOriginalBusinessDocumentReferenceIdentityAsync(xmlReader).ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsEndElement(payloadElementName))
                {
                    return transactionRecord;
                }

                readPayload = await xmlReader.ReadAsync().ConfigureAwait(false);
            }

            return transactionRecord;
        }

        private static Task<(string Identification, string Role)> ReadSenderEnergyPartyAsync(XmlReader xmlReader)
        {
            return ReadEnergyPartyAsync(xmlReader);
        }

        private static Task<(string Identification, string Role)> ReadRecipientEnergyPartyAsync(XmlReader xmlReader)
        {
            return ReadEnergyPartyAsync(xmlReader);
        }

        private static async Task<string> ReadOriginalBusinessDocumentReferenceIdentityAsync(XmlReader xmlReader)
        {
            var originalBusinessDocumentReferenceIdentityElement = xmlReader.LocalName;
            var readElement = true;

            while (readElement)
            {
                if (xmlReader.IsElement("Identification"))
                {
                    return await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                }

                if (xmlReader.IsEndElement(originalBusinessDocumentReferenceIdentityElement))
                {
                    break;
                }

                readElement = await xmlReader.ReadAsync().ConfigureAwait(false);
            }

            return string.Empty;
        }

        private static async Task<(string Identification, string Role)> ReadEnergyPartyAsync(XmlReader xmlReader)
        {
            var result = (Identification: string.Empty, Role: string.Empty);

            var energyPartyElement = xmlReader.LocalName;
            var readElements = true;

            while (readElements)
            {
                if (xmlReader.IsElement("Identification"))
                {
                    result.Identification = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsElement("Role"))
                {
                    result.Role = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsEndElement(energyPartyElement))
                {
                    break;
                }

                readElements = await xmlReader.ReadAsync().ConfigureAwait(false);
            }

            return result;
        }

        private static async Task<(string ProcessType, string BusinessSectorType)> ReadProcessEnergyContextAsync(XmlReader xmlReader)
        {
            var processType = string.Empty;
            var businessSectorType = string.Empty;
            var processContextElement = xmlReader.LocalName;
            const string processTypeElementName = "EnergyBusinessProcess";
            const string businessSectorTypeElementName = "EnergyIndustryClassification";
            var readElements = true;

            while (readElements)
            {
                if (xmlReader.IsElement(processTypeElementName))
                {
                    processType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsElement(businessSectorTypeElementName))
                {
                    businessSectorType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.IsEndElement(processContextElement))
                {
                    break;
                }

                readElements = await xmlReader.ReadAsync().ConfigureAwait(false);
            }

            return (processType, businessSectorType);
        }

        private static string ReadRsmName(string documentName)
        {
            var indexOfSeparator = documentName.IndexOf("_", StringComparison.CurrentCultureIgnoreCase);
            if (indexOfSeparator >= 0)
            {
                var startIndex = indexOfSeparator + 1;
                var rsmName = documentName.Substring(indexOfSeparator + 1, documentName.Length - startIndex);
#pragma warning disable CA1308
                return rsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }

            return documentName;
        }
    }
}
