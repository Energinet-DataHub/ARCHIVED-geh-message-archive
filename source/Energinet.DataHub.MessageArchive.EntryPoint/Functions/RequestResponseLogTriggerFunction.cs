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

using System.Diagnostics;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Processing.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Functions
{
    public sealed class RequestResponseLogTriggerFunction
    {
        private const string FunctionName = nameof(RequestResponseLogTriggerFunction);
        private readonly IBlobProcessingHandler _blobProcessingHandler;
        private readonly ILogger<RequestResponseLogTriggerFunction> _logger;

        public RequestResponseLogTriggerFunction(
            IBlobProcessingHandler blobProcessingHandler,
            ILogger<RequestResponseLogTriggerFunction> logger)
        {
            _blobProcessingHandler = blobProcessingHandler;
            _logger = logger;
        }

        [Function(FunctionName)]
        public async Task RunAsync(
            [TimerTrigger("*/5 */1 * * * *")]
            FunctionContext context)
        {
            _logger.LogInformation("RequestResponseLogTriggerFunction starting");
            var stopWatch = Stopwatch.StartNew();

            await _blobProcessingHandler.HandleAsync().ConfigureAwait(false);

            stopWatch.Stop();
            _logger.LogInformation("RequestResponseLogTriggerFunction executed, time ms: {Time}", stopWatch.ElapsedMilliseconds);
        }
    }
}
