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

using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Energinet.DataHub.MessageArchive.IntegrationTests
{
    internal sealed class CosmosDbFixture : IAsyncLifetime
    {
        public async Task InitializeAsync()
        {
            using var cosmosClient = new CosmosClient(LocalSettings.ConnectionString);

            var databaseResponse = await cosmosClient
                .CreateDatabaseIfNotExistsAsync(LocalSettings.DatabaseName)
                .ConfigureAwait(false);

            var testDatabase = databaseResponse.Database;

            await testDatabase
                .CreateContainerIfNotExistsAsync("logs", "/partitionKey")
                .ConfigureAwait(false);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
