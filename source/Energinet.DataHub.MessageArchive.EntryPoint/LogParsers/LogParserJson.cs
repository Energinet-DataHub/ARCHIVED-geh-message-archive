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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers
{
    public class LogParserJson : ILogParser
    {
        public BaseParsedModel Parse(BlobItemData blobItemData)
        {
            Guard.ThrowIfNull(blobItemData, nameof(blobItemData));

            var mridValue = string.Empty;
            var typeValue = string.Empty;
            var processTypeValue = string.Empty;
            var businessSectorTypeValue = string.Empty;
            var senderGlnValue = string.Empty;
            var senderMarketRoleValue = string.Empty;
            var receiverGlnValue = string.Empty;
            var receiverMarketRoleValue = string.Empty;
            var createdDataValue = string.Empty;

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
                LogCreatedDate = blobItemData.BlobCreatedOn.GetValueOrDefault().DateTime.ToString("u", CultureInfo.InvariantCulture),
                BlobContentUri = blobItemData.Uri.AbsoluteUri,
                HttpData = blobItemData.MetaData.TryGetValue("httpdatatype", out var httpdatatype) ? httpdatatype : string.Empty,
                InvocationId = blobItemData.MetaData.TryGetValue("invocationid", out var invocationid) ? invocationid : string.Empty,
                FunctionName = blobItemData.MetaData.TryGetValue("functionname", out var functionname) ? functionname : string.Empty,
                TraceId = blobItemData.MetaData.TryGetValue("traceid", out var traceid) ? traceid : string.Empty,
                TraceParent = blobItemData.MetaData.TryGetValue("traceparent", out var traceparent) ? traceparent : string.Empty,
                ResponseStatus = blobItemData.MetaData.TryGetValue("statuscode", out var statuscode) ? statuscode : string.Empty,
            };

            parsedModel.Data = blobItemData.IndexTags.Any() ? blobItemData.IndexTags : null;

            parsedModel.Errors = ParseErrors(blobItemData.Content);

            return parsedModel;
        }

        private static IEnumerable<ParsedErrorModel>? ParseErrors(string jsonString)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(jsonString);
                var errorPropParsed = jsonDocument.RootElement.TryGetProperty("error", out JsonElement errorProp);
                var code = errorProp.GetProperty("code").GetString();
                var message = errorProp.GetProperty("message").GetString();
                if (errorPropParsed)
                {
                    return new List<ParsedErrorModel>() { new () { Code = code ?? string.Empty, Message = message ?? string.Empty } };
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
