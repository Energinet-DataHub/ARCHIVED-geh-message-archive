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
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using Energinet.DataHub.MessageArchive.Client.Abstractions.Models;
using Energinet.DataHub.MessageArchive.Client.Utilities;

namespace Energinet.DataHub.MessageArchive.Client.Helpers
{
    internal static class QueryStringHelper
    {
        public static string BuildQueryString(SearchCriteria sc)
        {
            Guard.ThrowIfNull(sc, nameof(sc));

            var nameValues = HttpUtility.ParseQueryString(string.Empty);

            AddOnValue(nameValues, "messageId", sc.MessageId);
            AddOnValue(nameValues, "messageType", sc.MessageType);
            AddOnValue(nameValues, "functionName", sc.FunctionName);
            AddOnValue(nameValues, "invocationId", sc.InvocationId);
            AddOnValue(nameValues, "processType", sc.ProcessType);
            AddOnValue(nameValues, "reasonCode", sc.ReasonCode);
            AddOnValue(nameValues, "referenceId", sc.ReferenceId);
            AddOnValue(nameValues, "senderId", sc.SenderId);
            AddOnValue(nameValues, "senderId", sc.TraceId);
            AddOnValue(nameValues, "businessSectorType", sc.BusinessSectorType);

            if (sc.DateTimeFrom is null || sc.DateTimeTo is null)
            {
                var fromDate = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                AddOnValue(nameValues, "dateTimeFrom", fromDate);
                AddOnValue(nameValues, "dateTimeTo", toDate);
            }
            else
            {
                AddOnValue(nameValues, "dateTimeFrom", sc.DateTimeFrom);
                AddOnValue(nameValues, "dateTimeTo", sc.DateTimeTo);
            }

            return nameValues.ToString() ?? string.Empty;
        }

        private static void AddOnValue(NameValueCollection nv, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                nv.Add(name, value);
            }
        }
    }
}
