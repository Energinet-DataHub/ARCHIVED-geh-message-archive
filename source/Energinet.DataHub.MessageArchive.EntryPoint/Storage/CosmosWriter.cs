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
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Storage
{
    public class CosmosWriter : IStorageWriter<CosmosRequestResponseLog>,  IDisposable
    {
        private readonly string _databaseId;
        private readonly string _containerName;
        private readonly CosmosClient _cosmosClient;

        public CosmosWriter(string connectionString, string databaseId, string containerName)
        {
            _databaseId = databaseId;
            _containerName = containerName;
            _cosmosClient = new CosmosClientBuilder(connectionString)
                .WithSerializerOptions(new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase })
                .Build();
        }

        public async Task WriteAsync(CosmosRequestResponseLog objectToSave)
        {
            Guard.ThrowIfNull(objectToSave, nameof(objectToSave));

            objectToSave.Id = Guid.NewGuid().ToString(); // $"{objectToSave.InvocationId}_{objectToSave.MessageId}";
            objectToSave.PartitionKey = !string.IsNullOrWhiteSpace(objectToSave.ReceiverGln) ? objectToSave.ReceiverGln : "nopartitionkey";
            var container = _cosmosClient.GetContainer(_databaseId, _containerName);
            var response = await container.CreateItemAsync(objectToSave, new PartitionKey(objectToSave.PartitionKey)).ConfigureAwait(false);

            if (response.StatusCode is not HttpStatusCode.Created)
            {
                throw new InvalidOperationException($"CosmosWriter error {response.StatusCode.ToString()}");
            }
        }

        public void Dispose()
        {
            _cosmosClient.Dispose();
        }
    }
}
