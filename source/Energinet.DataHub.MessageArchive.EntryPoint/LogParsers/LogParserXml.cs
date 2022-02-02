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
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.ErrorParsers;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.Utilities;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers
{
    public class LogParserXml : LogParserBlobProperties
    {
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
                var senderGlnValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.SenderMarketParticipantmRid}");
                var senderMarketRoleValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.SenderMarketParticipantmarketRoletype}");
                var receiverGlnValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ReceiverMarketParticipantmRid}");
                var receiverMarketRoleValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.ReceiverMarketParticipantmarketRoletype}");
                var createdDataValue = ReadValueOrEmptyString(xmlDocument, $"{ns + ElementNames.CreatedDateTime}");

                parsedModel.MessageId = mridValue;
                parsedModel.MessageType = typeValue;
                parsedModel.ProcessType = processTypeValue;
                parsedModel.BusinessSectorType = businessSectorTypeValue;
                parsedModel.SenderGln = senderGlnValue;
                parsedModel.SenderGlnMarketRoleType = senderMarketRoleValue;
                parsedModel.ReceiverGln = receiverGlnValue;
                parsedModel.ReceiverGlnMarketRoleType = receiverMarketRoleValue;
                parsedModel.CreatedDate = createdDataValue;
            }
            catch
            {
            }

            return parsedModel;
        }

        private static string ReadValueOrEmptyString(XElement xmlDocument, string name)
        {
            var node = xmlDocument.Elements(name).FirstOrDefault();
            var value = node?.Value ?? string.Empty;
            return value;
        }
    }
}
