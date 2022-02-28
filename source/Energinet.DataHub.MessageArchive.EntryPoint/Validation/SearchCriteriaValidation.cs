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
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Validation
{
    public static class SearchCriteriaValidation
    {
        public static (bool Valid, string ErrorMessage) Validate(SearchCriteria searchCriteria)
        {
            Guard.ThrowIfNull(searchCriteria, nameof(searchCriteria));

            var datetimeValidation = ValidateDateTime(searchCriteria);
            if (!datetimeValidation.Valid)
            {
                searchCriteria.DateTimeFrom = null;
                searchCriteria.DateTimeTo = null;
                return (datetimeValidation.Valid, datetimeValidation.ErrorMessage);
            }

            return (true, string.Empty);
        }

        private static (bool Valid, string ErrorMessage) ValidateDateTime(SearchCriteria sc)
        {
            try
            {
                if (sc.DateTimeFrom is null || sc.DateTimeTo is null)
                {
                    return (false, "From and to date should be set");
                }

                var createdDateFromParsed = DateTime.TryParse(sc.DateTimeFrom, out var createdDateFromResult);
                var createdDateToParsed = DateTime.TryParse(sc.DateTimeTo, out var createdDateToResult);

                if (createdDateFromParsed && createdDateToParsed)
                {
                    sc.DateTimeFromParsed = new DateTime(createdDateFromResult.Year, createdDateFromResult.Month, createdDateFromResult.Day, 0, 0, 0).ToUniversalTime();
                    sc.DateTimeToParsed = new DateTime(createdDateToResult.Year, createdDateToResult.Month, createdDateToResult.Day, 23, 0, 0, DateTimeKind.Utc);
                    return (true, string.Empty);
                }

                return (false, $"date time parse error, from date parsed: {createdDateFromParsed}, to date parsed: {createdDateToParsed}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
