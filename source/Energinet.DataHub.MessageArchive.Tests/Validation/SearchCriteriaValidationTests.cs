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
using Energinet.DataHub.MessageArchive.Domain.Models;
using Energinet.DataHub.MessageArchive.Domain.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.Validation
{
    [UnitTest]
    public sealed class SearchCriteriaValidationTests
    {
        [Fact]
        public void Test_SearchParams_InvalidDateFrom()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = string.Empty;

            // Act
            var result = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.False(result.Valid);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseOnlyDate()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01T15:45:34Z";
            searchCriteria.DateTimeTo = "2022-01-19T15:45:34Z";

            DateTimeOffset logCreatedDate = new DateTime(637782039340000000, DateTimeKind.Utc); // 2022-01-19 15:45:34

            // Act
            var result = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.True(result.Valid);
            Assert.True(searchCriteria.DateTimeFromParsed <= logCreatedDate);
            Assert.True(searchCriteria.DateTimeToParsed!.Value.Ticks >= logCreatedDate.Ticks);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseFail()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01";
            searchCriteria.DateTimeTo = "2022-01-19 11?00:01";

            // Act
            var result = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.False(result.Valid);
            Assert.Null(searchCriteria.DateTimeToParsed);
            Assert.Null(searchCriteria.DateTimeToParsed);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseUTC_OK()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01T20:20:20Z";
            searchCriteria.DateTimeTo = "2022-01-19T15:45:34Z";

            DateTimeOffset logCreatedDate = new DateTime(637782039340000000, DateTimeKind.Utc); // 2022-01-19 15:45:34
#pragma warning disable CA1305
            var logCreatedDateString = logCreatedDate.ToString();
#pragma warning restore CA1305

            // Act
            var result = SearchCriteriaValidation.Validate(searchCriteria);

            var dateTimeParsedString = searchCriteria.DateTimeToParsed.ToString();

            // Assert
            Assert.True(result.Valid);
            Assert.True(searchCriteria.DateTimeFromParsed <= logCreatedDate);
            Assert.True(searchCriteria.DateTimeToParsed >= logCreatedDate);
            Assert.True(dateTimeParsedString == logCreatedDateString);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseUTC_MaxTime()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01T00:00:00.000+01:00";
            searchCriteria.DateTimeTo = "2022-01-19T23:59:59.000+01:00";

            DateTimeOffset logCreatedDate = new DateTime(637782039340000000, DateTimeKind.Utc); // 2022-01-19 15:45:34

            // Act
            var result = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.True(result.Valid);
            Assert.True(searchCriteria.DateTimeFromParsed <= logCreatedDate);
            Assert.True(searchCriteria.DateTimeToParsed >= logCreatedDate);
            Assert.True(searchCriteria.DateTimeToParsed!.Value.TimeOfDay == new TimeSpan(22, 59, 59));
        }

        private static SearchCriteria Create_ValidSearchCriteria()
        {
            return new SearchCriteria()
            {
                MessageId = "messageId",
                MessageType = "messageType",
                BusinessSectorType = "businessSectorType",
                DateTimeFrom = "2022-01-01",
                DateTimeTo = "2022-01-31",
                FunctionName = "functionName",
                InvocationId = "invocationId",
                ProcessType = "processType",
                ReasonCode = "reasonCode",
                IncludeRelated = false,
                SenderId = "senderId",
                ReceiverId = "receiverId",
                TraceId = "traceId",
            };
        }
    }
}
