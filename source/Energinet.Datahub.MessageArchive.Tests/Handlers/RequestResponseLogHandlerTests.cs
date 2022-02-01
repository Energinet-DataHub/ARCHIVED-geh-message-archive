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
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.BlobServices;
using Energinet.DataHub.MessageArchive.EntryPoint.Handlers;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Storage;
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
            var errorArchive = new Mock<IBlobErrorArchive>();

            var writer = new Mock<IStorageWriter<CosmosRequestResponseLog>>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;

            reader
                .Setup(e => e.GetBlobsReadyForProcessingAsync())
                .ReturnsAsync(
                    new List<BlobItemData>()
                    {
                        BlobItemData("xml", "<ok></ok>"),
                        BlobItemData("xml", "<notok><//notok>"),
                        BlobItemData("xml", "<Error><Code>1</Code><Message>test</Message></Error>"),
                        BlobItemData("json", "{\"error\":{\"code\":\"1\",\"message\":\"test\"}}"),
                        BlobItemData("json", "{\'bad\":{\"code\":\"1\",\"message\":\"test\"}}"),
                        BlobItemData("json", "{}"),
                        BlobItemData("text/plain", string.Empty),
                        BlobItemData("nocontent", string.Empty),
                    });

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(BlobItemData("txt", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, errorArchive.Object, writer.Object, logger);

            // Assert
            await handler.HandleAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_ErrorParsing()
        {
            // Arrange
            var reader = new Mock<IBlobReader>();
            var archive = new Mock<IBlobArchive>();
            var errorArchive = new Mock<IBlobErrorArchive>();

            var writer = new Mock<IStorageWriter<CosmosRequestResponseLog>>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;

            var blobItemErrorResponseXml = BlobItemData("xml", "<Error><Code>1</Code><Message>test</Message></Error>");
            blobItemErrorResponseXml.MetaData.Add("statuscode", HttpStatusCode.InternalServerError.ToString());
            var blobItemErrorResponseJson = BlobItemData("json", "{\"error\":{\"code\":\"1\",\"message\":\"test\"}}");
            blobItemErrorResponseJson.MetaData.Add("statuscode", HttpStatusCode.InternalServerError.ToString());

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
                .ReturnsAsync(BlobItemData("txt", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, errorArchive.Object, writer.Object, logger);
            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
        }

        [Fact]
        public async Task Test_NoParserFound()
        {
            // Arrange
            var reader = new Mock<IBlobReader>();
            var archive = new Mock<IBlobArchive>();
            var errorArchive = new Mock<IBlobErrorArchive>();

            var writer = new Mock<IStorageWriter<CosmosRequestResponseLog>>();
            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;

            var blobItemErrorResponseXml = BlobItemData("noparserfound", "no parser");

            reader
                .Setup(e => e.GetBlobsReadyForProcessingAsync())
                .ReturnsAsync(
                    new List<BlobItemData>()
                    {
                        blobItemErrorResponseXml,
                    });

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(BlobItemData("txt", string.Empty).Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, errorArchive.Object, writer.Object, logger);
            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
        }

        private static BlobItemData BlobItemData(string contentType, string content)
        {
            var uri = new Uri("https://localhost/TestBlob");

            return new BlobItemData(
                It.IsAny<string>(),
                new Dictionary<string, string>() { { "contenttype", contentType } },
                new Dictionary<string, string>(),
                content,
                DateTimeOffset.Now,
                uri);
        }
    }
}
