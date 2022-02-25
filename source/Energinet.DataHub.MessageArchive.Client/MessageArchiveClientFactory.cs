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
using System.Linq;
using System.Net.Http;
using Energinet.DataHub.MessageArchive.Client.Abstractions.Storage;
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MessageArchive.Client
{
    public class MessageArchiveClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStorageHandler _storageHandler;

        public MessageArchiveClientFactory(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IStorageHandler storageHandler)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _storageHandler = storageHandler;
        }

        public MessageArchiveClient CreateClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            SetAuthorizationHeader(httpClient);
            return new MessageArchiveClient(httpClient, _storageHandler);
        }

        public MessageArchiveClient CreateClient(Uri baseUrl)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = baseUrl;
            SetAuthorizationHeader(httpClient);
            return new MessageArchiveClient(httpClient, _storageHandler);
        }

        private string GetAuthorizationHeaderValue()
        {
            return _httpContextAccessor.HttpContext.Request.Headers
                .Where(x => x.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.ToString())
                .Single();
        }

        private void SetAuthorizationHeader(HttpClient httpClient)
        {
            var authHeaderValue = GetAuthorizationHeaderValue();
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeaderValue);
        }
    }
}
