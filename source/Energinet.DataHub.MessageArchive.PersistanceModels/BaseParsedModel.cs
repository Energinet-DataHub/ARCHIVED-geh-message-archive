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

using System;
using System.Collections.Generic;

namespace Energinet.DataHub.MessageArchive.PersistenceModels
{
    public class BaseParsedModel
    {
        public string? MessageId { get; set; }

        public string? MessageType { get; set; }

        public string? ProcessType { get; set; }

        public string? BusinessSectorType { get; set; }

        public string? ReasonCode { get; set; }

        public DateTimeOffset? CreatedDate { get; set; }

        public DateTimeOffset? LogCreatedDate { get; set; }

        public string? SenderGln { get; set; }

        public string? SenderGlnMarketRoleType { get; set; }

        public string? ReceiverGln { get; set; }

        public string? ReceiverGlnMarketRoleType { get; set; }

#pragma warning disable CA1056
        public string BlobContentUri { get; set; } = string.Empty;
#pragma warning restore CA1056

        public string? HttpData { get; set; }

        public string? InvocationId { get; set; }

        public string? FunctionName { get; set; }

        public string? TraceId { get; set; }

        public string? TraceParent { get; set; }

        public string? ResponseStatus { get; set; }

        public IEnumerable<TransactionRecord>? TransactionRecords { get; set; }

        public string? RsmName { get; set; }

        public bool? HaveBodyContent { get; set; } = false;

        public bool? ParsingSuccess { get; set; } = true;

#pragma warning disable CA2227
        public IDictionary<string, string>? Data { get; set; }

        public IDictionary<string, string>? Query { get; set; }
#pragma warning restore CA2227

        public IEnumerable<ParsedErrorModel>? Errors { get; set; }
    }
}
