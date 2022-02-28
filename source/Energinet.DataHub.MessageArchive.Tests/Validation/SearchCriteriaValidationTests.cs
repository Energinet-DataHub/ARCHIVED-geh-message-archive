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
using Energinet.DataHub.MessageArchive.EntryPoint.Validation;
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
            var (valid, errorMessage) = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.False(valid);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseCorrect()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01";
            searchCriteria.DateTimeTo = "2022-01-19";

            var logCreatedDate = new DateTime(637782039347871701, DateTimeKind.Utc); // 2022-01-19 15:45:34

            // Act
            var (valid, errorMessage) = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.True(valid);
            Assert.True(searchCriteria.DateTimeFromParsed <= logCreatedDate);
            Assert.True(searchCriteria.DateTimeToParsed >= logCreatedDate);
        }

        [Fact]
        public void Test_SearchParams_DateTimeParseFail()
        {
            // Arrange
            var searchCriteria = Create_ValidSearchCriteria();
            searchCriteria.DateTimeFrom = "2022-01-01";
            searchCriteria.DateTimeTo = "2022-01-19 11?00:01";

            var logCreatedDate = new DateTime(637782039347871701, DateTimeKind.Utc); // 2022-01-19 15:45:34

            // Act
            var (valid, errorMessage) = SearchCriteriaValidation.Validate(searchCriteria);

            // Assert
            Assert.False(valid);
            Assert.Null(searchCriteria.DateTimeToParsed);
            Assert.Null(searchCriteria.DateTimeToParsed);
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
                ReferenceId = "1234",
                SenderId = "senderId",
                ReceiverId = "receiverId",
                TraceId = "traceId",
            };
        }
    }
}
