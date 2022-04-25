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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.LogParsers
{
    [UnitTest]
    public class LogParserTests
    {
        [Fact]
        public void Parse_XML_MktActivityRecord_TransactionIds()
        {
            // Arrange
            var filename = "assets/requestchangeaccountingpointcharacteristics.xml";
            var xml = File.ReadAllText(filename);
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.TransactionRecords);
            Assert.NotEmpty(parsed.TransactionRecords);
            Assert.True(4 == parsed.TransactionRecords.Count());
        }

        [Fact]
        public void Parse_XML_Series_TransactionIds()
        {
            // Arrange
            var filename = "assets/test-series-ids.xml";
            var xml = File.ReadAllText(filename);
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.TransactionRecords);
            Assert.NotEmpty(parsed.TransactionRecords);
            Assert.True(4 == parsed.TransactionRecords.Count());
        }

        [Fact]
        public void Parse_XML_MktActivityRecord_LinkedMessage()
        {
            // Arrange
            var xml = $"<message><MktActivityRecord><{ElementNames.MRid}>1234567</{ElementNames.MRid}><{ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid}>1234</{ElementNames.OriginalTransactionIdReferenceMktActivityRecordmRid}></MktActivityRecord></message>";
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);
            var originalTransactionIdReference = (parsed.TransactionRecords ?? throw new InvalidOperationException()).First().OriginalTransactionIdReferenceId;

            // Assert
            Assert.NotNull(originalTransactionIdReference);
            Assert.NotEmpty(originalTransactionIdReference);
        }

        [Fact]
        public void Parse_XML_Series_LinkedMessage()
        {
            // Arrange
            var xml = $"<message><Series><{ElementNames.MRid}>1234567</{ElementNames.MRid}><{ElementNames.OriginalTransactionIdReferenceSeriesmRid}>1234</{ElementNames.OriginalTransactionIdReferenceSeriesmRid}></Series></message>";
            var blobItem = MockedTypes.BlobItemData("xml", xml);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);
            var originalTransactionIdReference = (parsed.TransactionRecords ?? throw new InvalidOperationException()).First().OriginalTransactionIdReferenceId;

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
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.RsmName);
            Assert.Equal("notifybillingmasterdata", parsed.RsmName);
        }

        [Fact]
        public void Test_IndexTagsInMetaDataParsing()
        {
            var indexTagsCount = 10;
            var indexTags = BuildIndexTagsDic(indexTagsCount);
            var indexTagsJson = JsonSerializer.Serialize(indexTags);

            var blobItem = MockedTypes.BlobItemData("xml", string.Empty);
            blobItem.MetaData.Add("indextags", indexTagsJson);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.Data);
            Assert.Equal(indexTagsCount, parsed.Data.Count);
        }

        [Fact]
        public void Test_IndexTagsInMetaDataParsing_IndexTagsFallBack()
        {
            var indexTagsCount = 2;
            var indexTags = BuildIndexTagsDic(indexTagsCount);

            var blobItem = MockedTypes.BlobItemData("xml", string.Empty, indexTags);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.Data);
            Assert.Equal(indexTagsCount, parsed.Data.Count);
        }

        [Fact]
        public void Test_IndexTagsInMetaDataParsing_IndexTagsNull()
        {
            var blobItem = MockedTypes.BlobItemData("xml", string.Empty, null);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.Null(parsed.Data);
        }

        [Fact]
        public void Test_QueryTagsInMetaDataParsing()
        {
            var tagsCount = 10;
            var tags = BuildIndexTagsDic(tagsCount);
            var tagsJson = JsonSerializer.Serialize(tags);

            var blobItem = MockedTypes.BlobItemData("xml", string.Empty);
            blobItem.MetaData.Add("querytags", tagsJson);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.NotNull(parsed.Query);
            Assert.Equal(tagsCount, parsed.Query.Count);
        }

        [Fact]
        public void Test_QueryTagsInMetaDataParsing_Null()
        {
            var blobItem = MockedTypes.BlobItemData("xml", string.Empty, null);
            var xmlParser = new LogParserXml(new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Act
            var parsed = xmlParser.Parse(blobItem);

            // Assert
            Assert.Null(parsed.Query);
        }

        private static Dictionary<string, string> BuildIndexTagsDic(int count)
        {
            var dic = new Dictionary<string, string>();

            for (var i = 0; i < count; i++)
            {
                dic.Add(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            }

            return dic;
        }
    }
}
