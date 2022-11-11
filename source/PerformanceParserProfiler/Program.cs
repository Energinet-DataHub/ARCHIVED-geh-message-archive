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
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace PerformanceParserProfiler
{
    public static class Program
    {
#if !DEBUG
        private static async Task Main(string[] args)
        {
            if (args.Contains("dotmemory-ebix"))
            {
                var p = new EbixParseBenchmark();
                await p.ParseBenchmarkAsync().ConfigureAwait(false);
            }
            else if (args.Contains("dotmemory-json"))
            {
                var p = new JsonParseBenchmark();
                await p.ParseBenchmarkAsync().ConfigureAwait(false);
            }
            else
            {
                if (args.Contains("ebix"))
                {
                    var summary = BenchmarkRunner.Run<EbixParseBenchmark>();
                }
                else if (args.Contains("json"))
                {
                    var summary = BenchmarkRunner.Run<JsonParseBenchmark>();
                }
                else
                {
                    throw new ArgumentException("Choose parser to benchmark");
                }
            }
        }
#endif

#if DEBUG
        private static async Task Main(string[] args)
        {
            var p = new EbixParseBenchmark();
            await p.ParseBenchmarkAsync().ConfigureAwait(false);
        }
#endif
    }
}
