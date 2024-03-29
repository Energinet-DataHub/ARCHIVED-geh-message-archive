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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.Handlers;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Energinet.DataHub.MessageArchive.Processing.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.Handlers
{
    [UnitTest]
    public class RequestResponseLogHandlerTests
    {
        [Fact]
        public async Task Handler_TestCompleteFlow_Ok()
        {
            // Arrange
            var reader = new Mock<IBlobReader>();
            var archive = new Mock<IBlobArchive>();

            var storage = new MockedStorageWriter<CosmosRequestResponseLog>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;
            var parserLogger = new Mock<ILogger<LogParserBlobProperties>>().Object;

            var logsToParse = new List<BlobItemData>()
            {
                MockedTypes.BlobItemData("xml", "<ok></ok>"),
                MockedTypes.BlobItemData("xml", "<notok><//notok>"),
                MockedTypes.BlobItemData("xml", "<Error><Code>1</Code><Message>test</Message></Error>"),
                MockedTypes.BlobItemData("json", "{\"error\":{\"code\":\"1\",\"message\":\"test\"}}"),
                MockedTypes.BlobItemData("json", "{\'bad\":{\"code\":\"1\",\"message\":\"test\"}}"),
                MockedTypes.BlobItemData("json", "{}"),
                MockedTypes.BlobItemData("text/plain", string.Empty),
                MockedTypes.BlobItemData("nocontent", string.Empty),
            };

            reader
                .Setup(e => e.GetBlobsReadyForProcessingAsync())
                .ReturnsAsync(logsToParse);

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(MockedTypes.BlobItemData("txt", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, storage, logger, parserLogger);
            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
            var storageList = storage.Storage().ToList();
            Assert.Equal(logsToParse.Count, storageList.Count);
        }

        [Fact]
        public async Task Test_ErrorParsing()
        {
            // Arrange
            var reader = new Mock<IBlobReader>();
            var archive = new Mock<IBlobArchive>();

            var storage = new MockedStorageWriter<CosmosRequestResponseLog>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;
            var parserLogger = new Mock<ILogger<LogParserBlobProperties>>().Object;

            var blobItemErrorResponseXml = MockedTypes.BlobItemData("xml", "<Error><Code>1</Code><Message>test</Message></Error>", null, HttpStatusCode.InternalServerError.ToString());
            var blobItemErrorResponseJson = MockedTypes.BlobItemData("json", "{\"error\":{\"code\":\"1\",\"message\":\"test\"}}", null, HttpStatusCode.InternalServerError.ToString());

            reader
                .Setup(e => e.GetBlobsReadyForProcessingAsync())
                .ReturnsAsync(
                    new List<BlobItemData>()
                    {
                        blobItemErrorResponseXml,
                        blobItemErrorResponseJson,
                    });

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(MockedTypes.BlobItemData("txt", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, storage, logger, parserLogger);
            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
            var storageList = storage.Storage().ToList();
            Assert.NotNull(storageList.FirstOrDefault(
                e => e.Errors != null
                     && e.Errors.Any(f => f.Code.Equals("1", StringComparison.Ordinal) && f.Message.Equals("test", StringComparison.Ordinal))));
        }

        [Fact]
        public async Task Test_ErrorParsing_WithParsingException()
        {
            // Arrange
            var reader = new Mock<IBlobReader>();
            var archive = new Mock<IBlobArchive>();

            var storage = new MockedStorageWriter<CosmosRequestResponseLog>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;
            var parserLogger = new Mock<ILogger<LogParserBlobProperties>>().Object;

            const string contentString = "{\"error\"-{\"some wrong json\"-\"1\",\"message\":\"test\"}}";

            using var contentStream = new MemoryStream();
            using var writer = new StreamWriter(contentStream);
            await writer.WriteAsync(contentString).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            contentStream.Position = 0;

            var blobItemErrorResponseJson = MockedTypes.BlobItemDataStream("json", contentStream, null, HttpStatusCode.OK.ToString());

            reader
                .Setup(e => e.GetBlobsReadyForProcessingAsync())
                .ReturnsAsync(new List<BlobItemData> { blobItemErrorResponseJson });

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(MockedTypes.BlobItemData("json", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, storage, logger, parserLogger);
            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
            var storageList = storage.Storage().ToList();
            Assert.True(storageList.All(log => log.ParsingSuccess == false));
        }

        [Theory]
        [InlineData("xml", null, "<ok></ok>", typeof(LogParserXml))]
        [InlineData("xml", null, "<notok><//notok>", typeof(LogParserXml))]
        [InlineData("xml", HttpStatusCode.InternalServerError, "<Error><Code>1</Code><Message>test</Message></Error>", typeof(LogParserErrorResponseXml))]
        [InlineData("json", HttpStatusCode.InternalServerError, "{\"error\":{\"code\":\"1\",\"message\":\"test\"}}", typeof(LogParserErrorResponseJson))]
        [InlineData("json", null, "{\'bad\":{\"code\":\"1\",\"message\":\"test\"}}", typeof(LogParserJson))]
        [InlineData("json", null, "{}", typeof(LogParserJson))]
        [InlineData("text/plain", null, "", typeof(LogParserBlobProperties))]
        [InlineData("nocontent", null, "", typeof(LogParserBlobProperties))]
        [InlineData("nocontent", HttpStatusCode.InternalServerError, "", typeof(LogParserBlobProperties))]
        public void Test_FindParser(string contentType, HttpStatusCode? httpStatusCode, string content, Type expectedParser)
        {
            // Arrange
            var blobItemData = MockedTypes.BlobItemData(contentType, content, null, httpStatusCode?.ToString() ?? string.Empty);

            // Act
            var parser = ParserFinder.FindParser(blobItemData, new Mock<ILogger<LogParserBlobProperties>>().Object);

            // Assert
            Assert.IsType(expectedParser, parser);
        }
    }
}
