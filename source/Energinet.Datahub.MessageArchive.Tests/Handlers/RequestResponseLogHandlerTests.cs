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
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.BlobServices;
using Energinet.DataHub.MessageArchive.EntryPoint.Handlers;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Storage;
using Google.Protobuf.WellKnownTypes;
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
                .ReturnsAsync(RandomBlobItemData(4));

            archive
                .Setup(e => e.MoveToArchiveAsync(It.IsAny<BlobItemData>()))
                .ReturnsAsync(RandomBlobItemData(1).First().Uri);

            // Act
            var handler = new BlobProcessingHandler(reader.Object, archive.Object, errorArchive.Object, writer.Object, logger);

            await handler.HandleAsync().ConfigureAwait(false);

            // Assert
        }

        private static List<BlobItemData> RandomBlobItemData(int number)
        {
            var items = new List<BlobItemData>();
            for (var i = 0; i < number; i++)
            {
                var uri = new Uri("https://localhost/TestBlob");

                items.Add(new BlobItemData(
                    It.IsAny<string>(),
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    It.IsAny<string>(),
                    DateTimeOffset.Now,
                    uri));
            }

            return items;
        }
    }
}
