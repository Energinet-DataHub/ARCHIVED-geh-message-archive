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

using System.Dynamic;
using System.Threading.Tasks;
using System.Xml;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers
{
    public class LogParserXml : ILogParser
    {
        public BaseParsedModel Parse(BlobItemData blobItemData)
        {
            Guard.ThrowIfNull(blobItemData, nameof(blobItemData));

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(blobItemData.Content);

            var mridValue = ReadValueOrEmptyString(xmlDocument, "/cim:mRID");
            var typeValue = ReadValueOrEmptyString(xmlDocument, "/cim:type");
            var processTypeValue = ReadValueOrEmptyString(xmlDocument, "/cim:process.processType");
            var businessSectorTypeValue = ReadValueOrEmptyString(xmlDocument, "/cim:businessSector.type");
            var senderGlnValue = ReadValueOrEmptyString(xmlDocument, "/cim:sender_MarketParticipant.mRID");
            var senderMarketRoleValue = ReadValueOrEmptyString(xmlDocument, "/cim:sender_MarketParticipant.marketRole.type");
            var receiverGlnValue = ReadValueOrEmptyString(xmlDocument, "/cim:receiver_MarketParticipant.mRID");
            var receiverMarketRoleValue = ReadValueOrEmptyString(xmlDocument, "/cim:receiver_MarketParticipant.marketRole.type");
            var createdDataValue = ReadValueOrEmptyString(xmlDocument, "/cim:createdDateTime");

            var parsedModel = new BaseParsedModel
            {
                MessageId = mridValue,
                MessageType = typeValue,
                ProcessType = processTypeValue,
                BusinessSectorType = businessSectorTypeValue,
                SenderGln = senderGlnValue,
                SenderGlnMarketRoleType = senderMarketRoleValue,
                ReceiverGln = receiverGlnValue,
                ReceiverGlnMarketRoleType = receiverMarketRoleValue,
                CreatedDate = createdDataValue,
                LogCreatedDate = blobItemData.Properties.CreatedOn.ToString(),
                BlobContentUri = blobItemData.Uri.AbsoluteUri,
                HttpData = blobItemData.MetaData.TryGetValue("httpdatatype", out var httpdatatype) ? httpdatatype : string.Empty,
                InvocationId = blobItemData.MetaData.TryGetValue("invocationid", out var invocationid) ? invocationid : string.Empty,
                FunctionName = blobItemData.MetaData.TryGetValue("functionname", out var functionname) ? functionname : string.Empty,
                TraceId = blobItemData.MetaData.TryGetValue("traceid", out var traceid) ? traceid : string.Empty,
                TraceParent = blobItemData.MetaData.TryGetValue("traceparent", out var traceparent) ? traceparent : string.Empty,
            };

            return parsedModel;
        }

        private static string ReadValueOrEmptyString(XmlDocument xmlDocument, string xpath)
        {
            var node = xmlDocument.SelectSingleNode(xpath);
            var value = node?.Value ?? string.Empty;
            return value;
        }
    }
}
