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
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Microsoft.Extensions.Logging;

namespace PerformanceParserProfiler
{
    [MemoryDiagnoser]
    public class JsonParseBenchmark
    {
        private ILogger<LogParserBlobProperties> _logger;

        public JsonParseBenchmark()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            });

            _logger = loggerFactory.CreateLogger<LogParserBlobProperties>();
        }

        [Benchmark]
        public async Task ParseBenchmarkAsync()
        {
            // "c:/temp/assets/NotifyValidatedMeasureData_large.json", c:/temp/assets/multiactivityrecords_confirmrequestchangeofsupplier.json
            var filePathToTest = string.Empty;
            using var fileStream = new FileStream(filePathToTest, FileMode.Open);
            var jsonStreamParser = new LogParserJson(_logger);
            var blobItem = BlobItemDataStream("json", fileStream);
            var parsedModel = await jsonStreamParser.ParseAsync(blobItem).ConfigureAwait(false);
        }

        private static BlobItemData BlobItemDataStream(string contentType, Stream contentStream, IDictionary<string, string>? indexTags = null)
        {
            var uri = new Uri("https://localhost/TestBlob");

            var blobItem = new BlobItemData(
                "TestFile",
                new Dictionary<string, string>() { { "contenttype", contentType } },
                indexTags ?? new Dictionary<string, string>(),
                string.Empty,
                DateTimeOffset.Now,
                uri);

            blobItem.ContentStream = contentStream;
            return blobItem;
        }
    }
}
