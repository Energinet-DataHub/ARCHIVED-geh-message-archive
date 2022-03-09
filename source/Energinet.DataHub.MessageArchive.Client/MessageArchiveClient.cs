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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Client.Abstractions;
using Energinet.DataHub.MessageArchive.Client.Abstractions.Models;
using Energinet.DataHub.MessageArchive.Client.Utilities;

namespace Energinet.DataHub.MessageArchive.Client
{
    public class MessageArchiveClient : IMessageArchiveClient
    {
        private readonly HttpClient _httpClient;

        public MessageArchiveClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SearchResultsDto?> SearchLogsAsync(SearchCriteria searchCriteria)
        {
            Guard.ThrowIfNull(searchCriteria, nameof(searchCriteria));

            var queryString = Helpers.QueryStringHelper.BuildQueryString(searchCriteria);
            var queryFromBaseUrl = string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.Query)
                ? "?"
                : _httpClient.BaseAddress?.Query;

            var searchUriRelative = new Uri($"ArchiveSearchRequestListener/{queryFromBaseUrl}&{queryString}", UriKind.Relative);

            var response = await _httpClient.GetAsync(searchUriRelative).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException();

            if (!response.IsSuccessStatusCode) return null;

            if (response.StatusCode == HttpStatusCode.NoContent) return new SearchResultsDto();

            var searchResults = await response.Content
                .ReadFromJsonAsync<SearchResultsDto>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                    {
                        Converters = { new JsonStringEnumConverter(), },
                    }).ConfigureAwait(false);

            return searchResults;
        }

        public async Task<Stream> GetStreamFromStorageAsync(string blobname)
        {
            Guard.ThrowIfNull(blobname, nameof(blobname));

            var queryString = $"blobname={blobname}";

            var queryFromBaseUrl = string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.Query)
                ? "?"
                : _httpClient.BaseAddress?.Query;

            var searchUriRelative = new Uri($"ArchiveDownloadRequestResponseLog/{queryFromBaseUrl}&{queryString}", UriKind.Relative);

            var response = await _httpClient.GetAsync(searchUriRelative).ConfigureAwait(false);

            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }
    }
}
