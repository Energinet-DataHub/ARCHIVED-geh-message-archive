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
using System.Globalization;
using Energinet.DataHub.MessageArchive.Reader.Models;

namespace Energinet.DataHub.MessageArchive.Reader.Validation
{
    public static class SearchCriteriaValidation
    {
        public static SearchCriteriaValidationResult Validate(SearchCriteria searchCriteria)
        {
            ArgumentNullException.ThrowIfNull(searchCriteria, nameof(searchCriteria));

            var datetimeValidation = ValidateDateTime(searchCriteria);
            if (!datetimeValidation.Valid)
            {
                searchCriteria.DateTimeFrom = null;
                searchCriteria.DateTimeTo = null;
                return new SearchCriteriaValidationResult(datetimeValidation.Valid, datetimeValidation.ErrorMessage);
            }

            ValidateAndUpdateRsmName(searchCriteria);
            ValidateAndUpdateProcessTypes(searchCriteria);
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
            sc.RsmNames ??= new List<string>();

            for (var i = 0; i < sc.RsmNames.Count; i++)
            {
#pragma warning disable CA1308
                sc.RsmNames[i] = sc.RsmNames[i].ToLowerInvariant();
#pragma warning restore CA1308
            }
        }

        private static void ValidateAndUpdateProcessTypes(SearchCriteria sc)
        {
            sc.ProcessTypes ??= new List<string>();

            for (var i = 0; i < sc.ProcessTypes.Count; i++)
            {
                sc.ProcessTypes[i] = sc.ProcessTypes[i].ToUpperInvariant();
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
            if (DateTime.TryParse(datetime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                parsedResult = result;
                return true;
            }

            parsedResult = default;
            return false;
        }
    }
}
