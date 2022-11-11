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

using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Energinet.DataHub.MessageArchive.Processing.LogParsers;
using Microsoft.Extensions.Logging;

namespace PerformanceParserProfiler
{
    [MemoryDiagnoser]
    public class EbixParseBenchmark
    {
        private ILogger<LogParserBlobProperties> _logger;

        public EbixParseBenchmark()
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
            var filePathToTest = string.Empty;
            using var fileStream = new FileStream(filePathToTest, FileMode.Open);
            var ebixStreamParser = new LogParserEbix(_logger);
            var blobItem = BlobItemHelper.BlobItemDataStream("ebix", fileStream);
            var parsedModel = await ebixStreamParser.ParseAsync(blobItem).ConfigureAwait(false);
        }
    }
}
