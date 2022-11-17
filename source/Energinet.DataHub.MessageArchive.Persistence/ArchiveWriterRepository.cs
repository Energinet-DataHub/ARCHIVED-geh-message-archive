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
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Persistence
{
    public class ArchiveWriterRepository : IStorageWriter<CosmosRequestResponseLog>
    {
        private readonly IArchiveContainer _archiveContainer;
        private readonly ILogger<ArchiveWriterRepository> _logger;

        public ArchiveWriterRepository(
            IArchiveContainer archiveContainer,
            ILogger<ArchiveWriterRepository> logger)
        {
            _archiveContainer = archiveContainer;
            _logger = logger;
        }

        public async Task WriteAsync(CosmosRequestResponseLog objectToSave)
        {
            ArgumentNullException.ThrowIfNull(objectToSave, nameof(objectToSave));

            objectToSave.Id = Guid.NewGuid().ToString();
            objectToSave.PartitionKey = Guid.NewGuid().ToString();
            var container = _archiveContainer.Container;
            var response = await container.CreateItemAsync(objectToSave, new PartitionKey(objectToSave.PartitionKey)).ConfigureAwait(false);

            _logger.LogInformation($"{nameof(ArchiveWriterRepository)} cosmos write response code: {response.StatusCode}");

            if (response.StatusCode is not HttpStatusCode.Created)
            {
                _logger.LogError($"CosmosWriter error status code: {response.StatusCode.ToString()}, diagnostics: {response.Diagnostics.ToString()}");
                throw new InvalidOperationException($"CosmosWriter error {response.StatusCode.ToString()}");
            }
        }
    }
}
