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

namespace Energinet.DataHub.MessageArchive.EntryPoint.Models
{
    public sealed record SearchResult
    {
        public SearchResult(
            string? messageId,
            string? messageType,
            string? processId,
            long? dateTimeReceived,
            string? senderId,
            string? businessReasonCode)
        {
            MessageId = messageId;
            MessageType = messageType;
            ProcessId = processId;
            DateTimeReceived = dateTimeReceived;
            SenderId = senderId;
            BusinessReasonCode = businessReasonCode;
        }

        public string? MessageId { get; }
        public string? MessageType { get; }
        public string? ProcessId { get; }
        public long? DateTimeReceived { get; }
        public string? SenderId { get; }
        public string? BusinessReasonCode { get; }
    }
}
