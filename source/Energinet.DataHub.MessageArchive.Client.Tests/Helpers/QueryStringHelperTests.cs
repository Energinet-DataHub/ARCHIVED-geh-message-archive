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
using Energinet.DataHub.MessageArchive.Client.Abstractions.Models;
using Energinet.DataHub.MessageArchive.Client.Helpers;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Client.Tests.Helpers
{
    [UnitTest]
    public class QueryStringHelperTests
    {
        [Fact]
        public void Test_QueryStringBuilder()
        {
            // Arrange
            var searchCriteria = new MessageArchiveSearchCriteria
            {
                MessageId = "TestId",
                MessageType = "TestType",
                BusinessSectorType = "BusType",
            };

            // Act
            var queryString = QueryStringHelper.BuildQueryString(searchCriteria);

            // Assert
            Assert.Contains("messageId", queryString);
            Assert.Contains("TestId", queryString);

            Assert.Contains("messageType", queryString);
            Assert.Contains("TestType", queryString);

            Assert.Contains("businessSectorType", queryString);
            Assert.Contains("BusType", queryString);

            Assert.DoesNotContain("functionName", queryString);

            var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.Contains(toDate, queryString);
        }
    }
}
