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
using Energinet.DataHub.MessageArchive.Domain.Models;
using Energinet.DataHub.MessageArchive.Domain.Repositories;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Cosmos;

namespace Energinet.DataHub.MessageArchive.Persistence
{
    public class ArchiveWriterRepository : IStorageWriter<CosmosRequestResponseLog>
    {
        private readonly IArchiveContainer _archiveContainer;

        public ArchiveWriterRepository(IArchiveContainer archiveContainer)
        {
            _archiveContainer = archiveContainer;
        }

        public async Task WriteAsync(CosmosRequestResponseLog objectToSave)
        {
            Guard.ThrowIfNull(objectToSave, nameof(objectToSave));

            objectToSave.Id = Guid.NewGuid().ToString();
            objectToSave.PartitionKey = Guid.NewGuid().ToString();
            var container = _archiveContainer.Container;
            var response = await container.CreateItemAsync(objectToSave, new PartitionKey(objectToSave.PartitionKey)).ConfigureAwait(false);

            if (response.StatusCode is not HttpStatusCode.Created)
            {
                throw new InvalidOperationException($"CosmosWriter error {response.StatusCode.ToString()}");
            }
        }
    }
}
