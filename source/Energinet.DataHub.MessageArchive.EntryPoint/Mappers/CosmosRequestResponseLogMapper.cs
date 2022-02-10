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

using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Mappers
{
    public static class CosmosRequestResponseLogMapper
    {
        public static CosmosRequestResponseLog ToCosmosRequestResponseLog(BaseParsedModel fromobj)
        {
            Guard.ThrowIfNull(fromobj, nameof(fromobj));

            var toobj = new CosmosRequestResponseLog();
            toobj.MessageId = fromobj.MessageId;
            toobj.MessageType = fromobj.MessageType;
            toobj.ProcessType = fromobj.ProcessType;
            toobj.BusinessSectorType = fromobj.BusinessSectorType;
            toobj.ReasonCode = fromobj.ReasonCode;
            toobj.CreatedDate = fromobj.CreatedDate;
            toobj.LogCreatedDate = fromobj.LogCreatedDate;
            toobj.SenderGln = fromobj.SenderGln;
            toobj.SenderGlnMarketRoleType = fromobj.SenderGlnMarketRoleType;
            toobj.ReceiverGln = fromobj.ReceiverGln;
            toobj.ReceiverGlnMarketRoleType = fromobj.ReceiverGlnMarketRoleType;
            toobj.BlobContentUri = fromobj.BlobContentUri;
            toobj.HttpData = fromobj.HttpData;
            toobj.InvocationId = fromobj.InvocationId;
            toobj.FunctionName = fromobj.FunctionName;
            toobj.TraceId = fromobj.TraceId;
            toobj.TraceParent = fromobj.TraceParent;
            toobj.OriginalTransactionIDReferenceId = fromobj.OriginalTransactionIDReferenceId;

            toobj.ResponseStatus = fromobj.ResponseStatus;
            toobj.Errors = fromobj.Errors;
            toobj.Data = fromobj.Data;

            return toobj;
        }

        public static BaseParsedModel ToBaseParsedModels(CosmosRequestResponseLog fromobj)
        {
            Guard.ThrowIfNull(fromobj, nameof(fromobj));

            var toobj = new BaseParsedModel();
            toobj.MessageId = fromobj.MessageId;
            toobj.MessageType = fromobj.MessageType;
            toobj.ProcessType = fromobj.ProcessType;
            toobj.BusinessSectorType = fromobj.BusinessSectorType;
            toobj.ReasonCode = fromobj.ReasonCode;
            toobj.CreatedDate = fromobj.CreatedDate;
            toobj.LogCreatedDate = fromobj.LogCreatedDate;
            toobj.SenderGln = fromobj.SenderGln;
            toobj.SenderGlnMarketRoleType = fromobj.SenderGlnMarketRoleType;
            toobj.ReceiverGln = fromobj.ReceiverGln;
            toobj.ReceiverGlnMarketRoleType = fromobj.ReceiverGlnMarketRoleType;
            toobj.BlobContentUri = fromobj.BlobContentUri;
            toobj.HttpData = fromobj.HttpData;
            toobj.InvocationId = fromobj.InvocationId;
            toobj.FunctionName = fromobj.FunctionName;
            toobj.TraceId = fromobj.TraceId;
            toobj.TraceParent = fromobj.TraceParent;
            toobj.OriginalTransactionIDReferenceId = fromobj.OriginalTransactionIDReferenceId;

            toobj.ResponseStatus = fromobj.ResponseStatus;
            toobj.Errors = fromobj.Errors;
            toobj.Data = fromobj.Data;

            return toobj;
        }
    }
}
