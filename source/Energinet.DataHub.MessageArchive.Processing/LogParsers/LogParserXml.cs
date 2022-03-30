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

                var originalTransactionIdReferenceId = ReadOriginalTransactionIdReferenceId(xmlDocument, ns);
                var rsmName = ReadRsmName(xmlDocument);

                parsedModel.MessageId = mridValue;
                parsedModel.MessageType = typeValue;
                parsedModel.ProcessType = processTypeValue;
                parsedModel.BusinessSectorType = businessSectorTypeValue;
                parsedModel.ReasonCode = reasonCodeTypeValue;
                parsedModel.SenderGln = senderGlnValue;
                parsedModel.SenderGlnMarketRoleType = senderMarketRoleValue;
                parsedModel.ReceiverGln = receiverGlnValue;
                parsedModel.ReceiverGlnMarketRoleType = receiverMarketRoleValue;
                parsedModel.OriginalTransactionIDReferenceId = originalTransactionIdReferenceId;
                parsedModel.RsmName = rsmName;

                var createdDateParsed = DateTimeOffset.TryParse(createdDataValue, out var createdDataValueParsed);
                parsedModel.CreatedDate = createdDateParsed ? createdDataValueParsed : null;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _applicationLogging.LogWarning(ex, "Parse Error in LogParserXml, returning base model");
            }

            return parsedModel;
        }

        private static string ReadValueOrEmptyString(XElement xmlDocument, string name)
        {
            var node = xmlDocument.Elements(name).FirstOrDefault();
            var value = node?.Value ?? string.Empty;
            return value;
        }

        private static string ReadOriginalTransactionIdReferenceId(XElement xmlDocument, XNamespace ns)
        {
            var mktActivityRecord = xmlDocument.Elements($"{ns + "MktActivityRecord"}").FirstOrDefault();
            var seriesRecord = xmlDocument.Elements($"{ns + "Series"}").FirstOrDefault();

            if (mktActivityRecord is not null)
            {
                return ReadValueOrEmptyString(
                    mktActivityRecord,
                    $"{ns + ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid}");
            }

            if (seriesRecord is not null)
            {
                return ReadValueOrEmptyString(
                    seriesRecord,
                    $"{ns + ElementNames.OriginalTransactionIdReferenceSeriesmRid}");
            }

            return string.Empty;
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
