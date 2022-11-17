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
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public class LogParserBlobProperties : ILogParser
    {
        /// <summary>
        /// Parses known and important properties from blob data. Including index tags.
        /// </summary>
        /// <param name="blobItemData"></param>
        /// <returns>Parsed blob data</returns>
        public virtual Task<BaseParsedModel> ParseAsync(BlobItemData blobItemData)
        {
            ArgumentNullException.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = new BaseParsedModel
            {
                MessageId = string.Empty,
                MessageType = string.Empty,
                ProcessType = string.Empty,
                BusinessSectorType = string.Empty,
                ReasonCode = string.Empty,
                SenderGln = string.Empty,
                SenderGlnMarketRoleType = string.Empty,
                ReceiverGln = string.Empty,
                ReceiverGlnMarketRoleType = string.Empty,
                CreatedDate = blobItemData.BlobCreatedOn,
                LogCreatedDate = blobItemData.BlobCreatedOn,
                BlobContentUri = blobItemData.Uri.AbsoluteUri,
                HttpData = blobItemData.MetaData.TryGetValue("httpdatatype", out var httpdatatype) ? httpdatatype : string.Empty,
                InvocationId = blobItemData.MetaData.TryGetValue("invocationid", out var invocationid) ? invocationid : string.Empty,
                FunctionName = blobItemData.MetaData.TryGetValue("functionname", out var functionname) ? functionname : string.Empty,
                TraceId = blobItemData.MetaData.TryGetValue("traceid", out var traceid) ? traceid : string.Empty,
                TraceParent = blobItemData.MetaData.TryGetValue("traceparent", out var traceparent) ? traceparent : string.Empty,
                ResponseStatus = blobItemData.MetaData.TryGetValue("statuscode", out var statuscode) ? statuscode : string.Empty,
                Data = Utilities.ParseTags.ParseIndexTagsElement(blobItemData),
                Query = Utilities.ParseTags.ParseQueryTagsElement(blobItemData),
            };

            if (blobItemData.ContentLength is > 0)
            {
                parsedModel.HaveBodyContent = true;
            }

            return Task.FromResult(parsedModel);
        }
    }
}
