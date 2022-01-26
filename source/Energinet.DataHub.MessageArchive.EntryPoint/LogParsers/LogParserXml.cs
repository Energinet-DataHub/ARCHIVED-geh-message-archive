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
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
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

        private static string ReadValueOrEmptyString(XElement xmlDocument, string xpath)
        {
            var node = xmlDocument.Elements(xpath).FirstOrDefault();
            var value = node?.Value ?? string.Empty;
            return value;
        }
    }
}
