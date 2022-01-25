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
using Energinet.DataHub.MessageArchive.EntryPoint.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Functions
{
    public sealed class RequestResponseLogTriggerFunction
    {
        private const string FunctionName = nameof(RequestResponseLogTriggerFunction);
        private readonly IBlobProcessingHandler _blobProcessingHandler;

        public RequestResponseLogTriggerFunction(IBlobProcessingHandler blobProcessingHandler)
        {
            _blobProcessingHandler = blobProcessingHandler;
        }

        [Function(FunctionName)]
        public async Task RunAsync(
            [TimerTrigger("* */1 * * * *")]
            FunctionContext context)
        {
            // Get blobs from storage
            // Read metadata, tags and content.
            // Define log type, CIM, JSON, REQUEST, RESPONSE, Peek, Post, Get
            // Build data object for saving to cosmos
            var logger = context.GetLogger<RequestResponseLogTriggerFunction>();
            var stopWatch = Stopwatch.StartNew();
            logger.LogInformation("RequestResponseLogTriggerFunction starting");

            await _blobProcessingHandler.HandleAsync().ConfigureAwait(false);

            stopWatch.Stop();
            logger.LogInformation("RequestResponseLogTriggerFunction executed, time ms: {time}", stopWatch.ElapsedMilliseconds);
        }
    }
}
