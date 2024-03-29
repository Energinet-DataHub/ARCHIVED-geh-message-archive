﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.MessageArchive.Client.Abstractions.Models
{
    public sealed record MessageArchiveSearchCriteria
    {
        public MessageArchiveSearchCriteria()
        {
        }

        public MessageArchiveSearchCriteria(
            string? messageId,
            string? messageType,
            string[]? processTypes,
            string? dateTimeFrom,
            string? dateTimeTo,
            string? senderId,
            string? receiverId,
            string? senderRoleType,
            string? receiverRoleType,
            string? businessSectorType,
            string? reasonCode,
            string? invocationId,
            string? functionName,
            string? traceId,
            bool includeRelated,
            string[]? rsmNames)
        {
            MessageId = messageId;
            MessageType = messageType;
            ProcessTypes = processTypes;
            DateTimeFrom = dateTimeFrom;
            DateTimeTo = dateTimeTo;
            SenderId = senderId;
            ReceiverId = receiverId;
            SenderRoleType = senderRoleType;
            ReceiverRoleType = receiverRoleType;
            BusinessSectorType = businessSectorType;
            ReasonCode = reasonCode;
            InvocationId = invocationId;
            FunctionName = functionName;
            TraceId = traceId;
            IncludeRelated = includeRelated;
            RsmNames = rsmNames;
        }

        public string? MessageId { get; set; }

        public string? MessageType { get; set; }

        public string[]? ProcessTypes { get; set; }

        public string? DateTimeFrom { get; set; }

        public string? DateTimeTo { get; set; }

        public string? SenderId { get; set; }

        public string? ReceiverId { get; set; }

        public string? SenderRoleType { get; set; }

        public string? ReceiverRoleType { get; set; }

        public string? BusinessSectorType { get; set; }

        public string? ReasonCode { get; set; }

        public string? InvocationId { get; set; }

        public string? FunctionName { get; set; }

        public string? TraceId { get; set; }

        public bool IncludeRelated { get; set; }

        public bool IncludeResultsWithoutContent { get; set; }

        public string[]? RsmNames { get; set; }

        public string? ContinuationToken { get; set; }

        public int MaxItemCount { get; set; } = -1;
    }
}
