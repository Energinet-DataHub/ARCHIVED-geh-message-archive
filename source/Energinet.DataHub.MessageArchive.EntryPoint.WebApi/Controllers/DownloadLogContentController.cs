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
using System.Net.Mime;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Reader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("api/log")]
    public class DownloadLogContentController : ControllerBase
    {
        private readonly ILogger<DownloadLogContentController> _logger;
        private readonly IStorageStreamReader _storageStreamReader;

        public DownloadLogContentController(
            ILogger<DownloadLogContentController> logger,
            IStorageStreamReader storageStreamReader)
        {
            _logger = logger;
            _storageStreamReader = storageStreamReader;
        }

        [HttpGet("download/{logname}")]
        public async Task<ActionResult> DownloadAsync(string logname)
        {
            try
            {
                var blobNameToDownload = logname ?? throw new ArgumentNullException(nameof(logname));

                blobNameToDownload = Uri.UnescapeDataString(blobNameToDownload);

                var logStream = await _storageStreamReader
                    .GetStreamFromStorageAsync(blobNameToDownload)
                    .ConfigureAwait(false);

                if (logStream != Stream.Null)
                {
                    return File(logStream, MediaTypeNames.Application.Octet, BuildFileName(logname));
                }

                var response = Content(HttpStatusCode.NotFound.ToString());
                response.StatusCode = (int)HttpStatusCode.NotFound;

                return response;
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                _logger.LogError(e, "An error occurred while processing request");
                _logger.LogError(e.ToString());
                var response = Content("An error occured while processing the request.");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return response;
            }
        }

        private static string BuildFileName(string filename)
        {
            ArgumentNullException.ThrowIfNull(filename, nameof(filename));

            if (filename.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase) ||
                filename.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                return filename;
            }

            return $"{filename}.txt";
        }
    }
}
