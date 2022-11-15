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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Energinet.DataHub.MessageArchive.Tests.LogParsers;

public class LogParserAssetsExpectedResultTests
{
    [Theory]
    [InlineData("requestchangeaccountingpointcharacteristics")]
    [InlineData("notifybillingmasterdata")]
    public async Task Parse_CompareWithExpectedResult_Xml(string assetsFileName)
    {
        await Parse_CompareWithJsonExpectedResult(assetsFileName, "xml").ConfigureAwait(false);
    }

    [Theory]
    [InlineData("requestchangeaccountingpointcharacteristics")]
    [InlineData("notifybillingmasterdata")]
    public async Task Parse_CompareWithJsonExpectedResult_Json(string assetsFileName)
    {
        await Parse_CompareWithJsonExpectedResult(assetsFileName, "json").ConfigureAwait(false);
    }

    [Theory]
    [InlineData("EbIX/DK_RequestEndOfSupply-1")]
    [InlineData("EbIX/DK_RequestEndOfSupply-2")]
    [InlineData("EbIX/DK_RequestEndOfSupply-3")]
    [InlineData("EbIX/DK_MeteredDataTimeSeries-1")]
    [InlineData("EbIX/DK_Acknowledgment-1")]
    [InlineData("EbIX/DK_ConfirmChangeOfSupplier-1")]
    [InlineData("EbIX/DK_NotifyChangeOfSupplier-1")]
    [InlineData("EbIX/DK_RequestChangeOfSupplier-1")]
    public async Task Parse_CompareWithExpectedResult_Ebix(string assetsFileName)
    {
        await Parse_CompareWithJsonExpectedResult(assetsFileName, "ebix.xml").ConfigureAwait(false);
    }

    private static async Task Parse_CompareWithJsonExpectedResult(string assetsFileName, string extensionAndContentType)
    {
        // Arrange
        var assetsPath = $"Assets/{assetsFileName}.{extensionAndContentType}";
        var parsedResultAssets = $"Assets/ExpectedParseResults/{assetsFileName}_result.json";

        var blobItem = await LoadFileAndSetBlobItemData(assetsPath, extensionAndContentType).ConfigureAwait(false);

        var contentParser = ParserFinder.FindParser(extensionAndContentType, "200", blobItem.Content, new Mock<ILogger<LogParserBlobProperties>>().Object);

        // Act
        var parsed = await contentParser.ParseAsync(blobItem).ConfigureAwait(false);

        // Assert
        var expectedResultJsonString = await File.ReadAllTextAsync(parsedResultAssets, Encoding.UTF8).ConfigureAwait(false);
        var resultJsonString = JsonSerializer.Serialize(parsed);

        using var expectedJsonReader = new JsonTextReader(new StringReader(expectedResultJsonString)) { DateParseHandling = DateParseHandling.None };
        using var resultJsonReader = new JsonTextReader(new StringReader(resultJsonString)) { DateParseHandling = DateParseHandling.None };

        var expectedJsonJObject = await JObject.LoadAsync(expectedJsonReader).ConfigureAwait(false);
        var resultJsonJObject = await JObject.LoadAsync(resultJsonReader).ConfigureAwait(false);

        expectedJsonJObject.Remove("LogCreatedDate");
        expectedJsonJObject.Remove("BlobContentUri");

        resultJsonJObject.Remove("LogCreatedDate");
        resultJsonJObject.Remove("BlobContentUri");

        var isEqual = JToken.DeepEquals(expectedJsonJObject, resultJsonJObject);

#if DEBUG
        if (!isEqual)
        {
            Debug.WriteLine(expectedResultJsonString);
            Debug.WriteLine(resultJsonString);
        }
#endif

        Assert.True(isEqual);
    }

    private static async Task<BlobItemData> LoadFileAndSetBlobItemData(string assetsPath, string extensionAndContentType)
    {
        ArgumentNullException.ThrowIfNull(extensionAndContentType);

        if (extensionAndContentType.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var fileStream = File.Open(assetsPath, FileMode.Open);
            return MockedTypes.BlobItemDataStream(extensionAndContentType, fileStream);
        }

        if (extensionAndContentType.Equals("ebix.xml", StringComparison.OrdinalIgnoreCase))
        {
            var fileStream = File.Open(assetsPath, FileMode.Open);
            return MockedTypes.BlobItemDataStream(extensionAndContentType, fileStream);
        }

        var fileContent = await File.ReadAllTextAsync(assetsPath, Encoding.UTF8).ConfigureAwait(false);

        return MockedTypes.BlobItemData(extensionAndContentType, fileContent);
    }
}
