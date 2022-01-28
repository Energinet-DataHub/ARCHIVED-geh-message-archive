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

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Energinet.DataHub.MessageArchive.EntryPoint.BlobServices;
using Energinet.DataHub.MessageArchive.EntryPoint.Handlers;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.Datahub.MessageArchive.Tests.Handlers
{
    [UnitTest]
    public class RequestResponseLogHandlerTests
    {
        [Fact]
        public async Task BlobProcessingHandler_Ok()
        {
            var conStringTEST = string.Empty;
            var conStringXKAMA = string.Empty;

            var blobConnectionString = conStringTEST;
            var blobContainerName = "marketoplogs";

            var cosmosConnectionString = string.Empty;
            var cosmosDatabaseId = "Search";
            var cosmosContainerName = "Logs";

            var reader = new BlobReader(blobConnectionString, blobContainerName);
            using var writer = new CosmosWriter(cosmosConnectionString, cosmosDatabaseId, cosmosContainerName);

            var logger = new Mock<ILogger<BlobProcessingHandler>>().Object;

            var handler = new BlobProcessingHandler(reader, writer, logger);

            await handler.HandleAsync().ConfigureAwait(false);
        }
    }
}
