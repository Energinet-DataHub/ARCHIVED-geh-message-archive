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
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Functions
{
    public class OverlapTestTimerTrigger
    {
        private const string FunctionName = nameof(OverlapTestTimerTrigger);

        private readonly ILogger<OverlapTestTimerTrigger> _logger;
        private readonly IArchiveContainer _container;

        public OverlapTestTimerTrigger(ILogger<OverlapTestTimerTrigger> logger, IArchiveContainer container)
        {
            _logger = logger;
            _container = container;
        }

        [Function(FunctionName)]
        public async Task RunAsync([TimerTrigger("*/5 * * * * *", UseMonitor = true)] FunctionContext context)
        {
            var guid = Guid.NewGuid().ToString();

            _logger.LogInformation($"OverlapTestTimerTrigger started {guid}");

            await Task.Delay(7000).ConfigureAwait(false);

            await _container.Container.CreateItemAsync(new OverlapTest
            {
                Id = guid,
                PartitionKey = "OverlapTest",
            }).ConfigureAwait(false);

            _logger.LogInformation($"OverlapTestTimerTrigger done {guid}");
        }

        private class OverlapTest
        {
            public string Id { get; set; } = null!;

            public string PartitionKey { get; set; } = null!;
        }
    }
}
