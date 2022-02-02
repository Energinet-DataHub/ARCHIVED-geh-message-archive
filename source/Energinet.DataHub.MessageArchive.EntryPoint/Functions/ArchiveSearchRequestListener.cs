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
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Functions
{
    public sealed class ArchiveSearchRequestListener
    {
        private readonly IArchiveReaderRepository _archiveReaderRepository;

        public ArchiveSearchRequestListener(IArchiveReaderRepository archiveReaderRepository)
        {
            _archiveReaderRepository = archiveReaderRepository;
        }

        [Function("ArchiveSearchRequestListener")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData request)
        {
            Guard.ThrowIfNull(request, nameof(request));

            using StreamReader streamReader = new (request.Body);

            var requestBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);

            var searchCriteria = JsonSerializer.Deserialize<SearchCriteria>(requestBody);
            if (searchCriteria is null)
            {
                throw new InvalidOperationException(nameof(searchCriteria));
            }

            var searchResults = await _archiveReaderRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(searchResults).ConfigureAwait(false);

            return searchResults.Results.Count > 0 ? response : request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
