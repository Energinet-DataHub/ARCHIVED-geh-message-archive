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

namespace Energinet.DataHub.MessageArchive.EntryPoint.Models
{
    public sealed record SearchCriteria
    {
        public SearchCriteria()
        {
        }

        public SearchCriteria(
            string? messageId,
            string? messageType,
            string? processType,
            long? dateTimeFrom,
            long? dateTimeTo,
            string? senderId,
            string? businessSectorType,
            string? reasonCode,
            string? invocationId,
            string? functionName,
            string? traceId,
            string? referenceId)
        {
            MessageId = messageId;
            MessageType = messageType;
            ProcessType = processType;
            DateTimeFrom = dateTimeFrom;
            DateTimeTo = dateTimeTo;
            SenderId = senderId;
            BusinessSectorType = businessSectorType;
            ReasonCode = reasonCode;
            InvocationId = invocationId;
            FunctionName = functionName;
            TraceId = traceId;
            ReferenceId = referenceId;
        }

        public string? MessageId { get; set; }
        public string? MessageType { get; set; }
        public string? ProcessType { get; set; }
        public long? DateTimeFrom { get; set; }
        public long? DateTimeTo { get; set; }
        public string? SenderId { get; set; }
        public string? BusinessSectorType { get; set; }
        public string? ReasonCode { get; set; }
        public string? InvocationId { get; set; }
        public string? FunctionName { get; set; }
        public string? TraceId { get; set; }
        public string? ReferenceId { get; set; }
    }
}
