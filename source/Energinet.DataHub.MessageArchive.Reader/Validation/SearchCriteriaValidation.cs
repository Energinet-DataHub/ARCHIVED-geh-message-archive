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
using System.Globalization;
using Energinet.DataHub.MessageArchive.Reader.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.Reader.Validation
{
    public static class SearchCriteriaValidation
    {
        public static SearchCriteriaValidationResult Validate(SearchCriteria searchCriteria)
        {
            Guard.ThrowIfNull(searchCriteria, nameof(searchCriteria));

            var datetimeValidation = ValidateDateTime(searchCriteria);
            if (!datetimeValidation.Valid)
            {
                searchCriteria.DateTimeFrom = null;
                searchCriteria.DateTimeTo = null;
                return new SearchCriteriaValidationResult(datetimeValidation.Valid, datetimeValidation.ErrorMessage);
            }

            ValidateAndUpdateRsmName(searchCriteria);
            ValidateIncludeRelated(searchCriteria);

            return new SearchCriteriaValidationResult(true);
        }

        private static void ValidateIncludeRelated(SearchCriteria sc)
        {
            if (sc.MessageId is null)
            {
                sc.IncludeRelated = false;
            }

            sc.IncludeRelated ??= false;
        }

        private static void ValidateAndUpdateRsmName(SearchCriteria sc)
        {
            if (!string.IsNullOrWhiteSpace(sc.RsmName))
            {
#pragma warning disable CA1308
                sc.RsmName = sc.RsmName.ToLowerInvariant();
#pragma warning restore CA1308
            }
        }

        private static SearchCriteriaValidationResult ValidateDateTime(SearchCriteria sc)
        {
            try
            {
                if (sc.DateTimeFrom is null || sc.DateTimeTo is null)
                {
                    return new SearchCriteriaValidationResult(false, "From and to date should be set");
                }

                var createdDateFromParsed = TryParseExactDateTimeStringAsIso(sc.DateTimeFrom, out var createdDateFromResult);
                var createdDateToParsed = TryParseExactDateTimeStringAsIso(sc.DateTimeTo, out var createdDateToResult);

                if (createdDateFromParsed && createdDateToParsed)
                {
                    sc.DateTimeFromParsed = createdDateFromResult.ToUniversalTime();
                    sc.DateTimeToParsed = createdDateToResult.ToUniversalTime();
                    return new SearchCriteriaValidationResult(true);
                }

                return new SearchCriteriaValidationResult(false, $"date time parse error, from date: {sc.DateTimeFrom}, to date: {sc.DateTimeTo}");
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                return new SearchCriteriaValidationResult(false, ex.Message);
            }
        }

        private static bool TryParseExactDateTimeStringAsIso(string datetime, out DateTimeOffset parsedResult)
        {
            if (DateTime.TryParseExact(datetime, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                parsedResult = result;
                return true;
            }

            parsedResult = default;
            return false;
        }

#pragma warning disable SA1201
        private static readonly string[] _formats =
        {
#pragma warning restore SA1201
            // Basic formats
            "yyyyMMddTHHmmsszzz",
            "yyyyMMddTHHmmsszz",
            "yyyyMMddTHHmmssZ",

            // Extended formats
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:sszz",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",

            // All of the above with reduced accuracy
            "yyyyMMddTHHmmzzz",
            "yyyyMMddTHHmmzz",
            "yyyyMMddTHHmmZ",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mmzz",
            "yyyy-MM-ddTHH:mmZ",

            // Accuracy reduced to hours
            "yyyyMMddTHHzzz",
            "yyyyMMddTHHzz",
            "yyyyMMddTHHZ",
            "yyyy-MM-ddTHHzzz",
            "yyyy-MM-ddTHHzz",
            "yyyy-MM-ddTHHZ",
        };
    }
}
