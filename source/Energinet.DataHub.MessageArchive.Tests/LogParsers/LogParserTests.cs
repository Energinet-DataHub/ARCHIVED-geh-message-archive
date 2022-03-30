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

using System.IO;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.Utilities;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.LogParsers
{
    [UnitTest]
    public class LogParserTests
    {
        [Fact]
        public void Parse_XML_MktActivityRecord_LinkedMessage()
        {
            // Arrange
            var xml = $"<message><MktActivityRecord><{ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid}>1234</{ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid}></MktActivityRecord></message>";
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml();

            // Act
            var parsed = xmlParser.Parse(blobItem);
            var originalTransactionIdReference = parsed.OriginalTransactionIDReferenceId;

            // Assert
            Assert.NotNull(originalTransactionIdReference);
            Assert.NotEmpty(originalTransactionIdReference);
        }

        [Fact]
        public void Parse_XML_Series_LinkedMessage()
        {
            // Arrange
            var xml = $"<message><Series><{ElementNames.OriginalTransactionIdReferenceSeriesmRid}>1234</{ElementNames.OriginalTransactionIdReferenceSeriesmRid}></Series></message>";
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml();

            // Act
            var parsed = xmlParser.Parse(blobItem);
            var originalTransactionIdReference = parsed.OriginalTransactionIDReferenceId;

            // Assert
            Assert.NotNull(originalTransactionIdReference);
            Assert.NotEmpty(originalTransactionIdReference);
        }

        [Fact]
        public void Test_RsmNameParsing()
        {
            var filename = "assets/notifybillingmasterdata.xml";
            var xml = File.ReadAllText(filename);
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml();

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.RsmName);
            Assert.Equal("notifybillingmasterdata", parsed.RsmName);
        }
    }
}
