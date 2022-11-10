using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Utilities;
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
            Guard.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = await base.ParseAsync(blobItemData).ConfigureAwait(false);

            if (blobItemData.ContentLength > 0 && blobItemData.ContentStream != null)
            {
                await ParseEbixFromStreamAsync(parsedModel, blobItemData.ContentStream).ConfigureAwait(false);
                parsedModel.CreatedDate ??= parsedModel.LogCreatedDate;
            }

            return parsedModel;
        }

        private static async Task ParseEbixFromStreamAsync(BaseParsedModel parsedModel, Stream contentStream)
        {
            using var xmlReader = XmlReader.Create(contentStream, _xmlReaderSettings);

            var tempTransactionRecords = new List<TransactionRecord>();

            while (await xmlReader.ReadAsync().ConfigureAwait(false))
            {
                Debug.WriteLine($"{xmlReader.Depth} - {xmlReader.NodeType} - {xmlReader.LocalName} - {xmlReader.Name}");

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
                        if (xmlReader.NodeType == XmlNodeType.Element
                            && xmlReader.LocalName.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                        {
                            parsedModel.MessageId = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            continue;
                        }

                        if (xmlReader.NodeType == XmlNodeType.Element
                            && xmlReader.LocalName.Equals("DocumentType", StringComparison.OrdinalIgnoreCase))
                        {
                            parsedModel.MessageType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            continue;
                        }

                        if (xmlReader.NodeType == XmlNodeType.Element
                            && xmlReader.LocalName.Equals("Creation", StringComparison.OrdinalIgnoreCase))
                        {
                            var dateString = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            parsedModel.CreatedDate = DateTimeOffset.TryParse(dateString, out var result) ? result : null;
                            continue;
                        }

                        if (xmlReader.NodeType == XmlNodeType.Element
                            && xmlReader.LocalName.Equals("SenderEnergyParty", StringComparison.OrdinalIgnoreCase))
                        {
                            var senderResult = await ReadSenderEnergyPartyAsync(xmlReader).ConfigureAwait(false);
                            parsedModel.SenderGln = senderResult.Identification;
                            parsedModel.SenderGlnMarketRoleType = senderResult.Role;
                            continue;
                        }

                        if (xmlReader.NodeType == XmlNodeType.Element
                            && xmlReader.LocalName.Equals("RecipientEnergyParty", StringComparison.OrdinalIgnoreCase))
                        {
                            var recipientResult = await ReadRecipientEnergyPartyAsync(xmlReader).ConfigureAwait(false);
                            parsedModel.ReceiverGln = recipientResult.Identification;
                            parsedModel.ReceiverGlnMarketRoleType = recipientResult.Role;
                            continue;
                        }

                        if (xmlReader.NodeType == XmlNodeType.EndElement
                            && xmlReader.LocalName.Equals("HeaderEnergyDocument", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        continueRead = await xmlReader.ReadAsync().ConfigureAwait(false);
                    }
                    while (continueRead);
                }

                if (xmlReader.LocalName.Equals("ProcessEnergyContext", StringComparison.OrdinalIgnoreCase))
                {
                    var energyContext = await ReadProcessEnergyContextAsync(xmlReader).ConfigureAwait(false);
                    parsedModel.ProcessType = energyContext.ProcessType;
                    parsedModel.BusinessSectorType = energyContext.BusinessSectorType;
                    continue;
                }

                if (xmlReader.LocalName.StartsWith("Payload", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionRecord = await ReadPayloadAsync(xmlReader).ConfigureAwait(false);
                    tempTransactionRecords.Add(transactionRecord);
                }
            }

            parsedModel.TransactionRecords = tempTransactionRecords.Count > 0 ? tempTransactionRecords : null;
        }

        private static async Task<TransactionRecord> ReadPayloadAsync(XmlReader xmlReader)
        {
            var payloadElementName = xmlReader.LocalName;
            var transactionRecord = new TransactionRecord() { OriginalTransactionIdReferenceId = string.Empty };
            var readPayload = true;

            while (readPayload)
            {
                if (xmlReader.Depth > 2) readPayload = await xmlReader.ReadAsync().ConfigureAwait(false);

                if (xmlReader.LocalName.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                {
                    transactionRecord.MRid = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.LocalName.Equals("OriginalBusinessDocumentReferenceIdentity", StringComparison.OrdinalIgnoreCase))
                {
                    transactionRecord.OriginalTransactionIdReferenceId = await ReadOriginalBusinessDocumentReferenceIdentityAsync(xmlReader).ConfigureAwait(false);
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
                if (xmlReader.LocalName.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                {
                    return await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                }

                if (xmlReader.NodeType == XmlNodeType.EndElement
                    && xmlReader.LocalName.Equals(originalBusinessDocumentReferenceIdentityElement, StringComparison.OrdinalIgnoreCase))
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
                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                {
                    result.Identification = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName.Equals("Role", StringComparison.OrdinalIgnoreCase))
                {
                    result.Role = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.EndElement
                    && xmlReader.LocalName.Equals(energyPartyElement, StringComparison.OrdinalIgnoreCase))
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
                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName == processTypeElementName)
                {
                    processType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Element
                    && xmlReader.LocalName == businessSectorTypeElementName)
                {
                    businessSectorType = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.EndElement
                    && xmlReader.LocalName.Equals(processContextElement, StringComparison.OrdinalIgnoreCase))
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
