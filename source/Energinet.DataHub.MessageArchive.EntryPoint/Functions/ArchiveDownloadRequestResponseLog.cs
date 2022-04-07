﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Net.Mime;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository;
using Energinet.DataHub.MessageArchive.EntryPoint.Utilities;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Functions
{
    public sealed class ArchiveDownloadRequestResponseLog
    {
        private readonly IStorageStreamReader _storageStreamReader;

        public ArchiveDownloadRequestResponseLog(IStorageStreamReader storageStreamReader)
        {
            _storageStreamReader = storageStreamReader;
        }

        [Function("ArchiveDownloadRequestResponseLog")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData request)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var parsedQueryString = System.Web.HttpUtility.ParseQueryString(request.Url.Query);

            var blobNameToDownload = parsedQueryString.Get("blobname") ?? throw new ArgumentNullException("blobname");

            var logStream = await _storageStreamReader
                .GetStreamFromStorageAsync(blobNameToDownload)
                .ConfigureAwait(false);

            var response = logStream != Stream.Null
                ? request.CreateResponse(logStream, MediaTypeNames.Text.Xml)
                : request.CreateResponse(HttpStatusCode.NoContent);

            return response;
        }
    }
}