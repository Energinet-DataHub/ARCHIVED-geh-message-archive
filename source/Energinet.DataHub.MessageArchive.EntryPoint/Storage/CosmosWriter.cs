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
using Microsoft.Azure.Cosmos;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Storage
{
    public class CosmosWriter : IStorageWriter<BaseParsedModel>,  IDisposable
    {
        private readonly string _databaseId;
        private readonly string _containerName;
        private readonly CosmosClient _cosmosClient;

        public CosmosWriter(string connectionString, string databaseId, string containerName)
        {
            _databaseId = databaseId;
            _containerName = containerName;
            _cosmosClient = new CosmosClient(connectionString);
        }

        public async Task WriteAsync(BaseParsedModel objectToSave)
        {
            var container = _cosmosClient.GetContainer(_databaseId, _containerName);
            var response = await container.CreateItemAsync(objectToSave).ConfigureAwait(false);

            if (response.StatusCode is not HttpStatusCode.OK)
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
