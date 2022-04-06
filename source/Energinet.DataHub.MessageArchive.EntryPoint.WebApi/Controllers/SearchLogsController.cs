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
using Energinet.DataHub.MessageArchive.Reader;
using Energinet.DataHub.MessageArchive.Reader.Models;
using Energinet.DataHub.MessageArchive.Reader.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("api/log")]
    public class SearchLogsController : ControllerBase
    {
        private readonly ILogger<SearchLogsController> _logger;
        private readonly IArchiveSearchRepository _archiveSearchRepository;

        public SearchLogsController(
            ILogger<SearchLogsController> logger,
            IArchiveSearchRepository archiveSearchRepository)
        {
            _logger = logger;
            _archiveSearchRepository = archiveSearchRepository;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAsync([FromQuery] SearchCriteria searchCriteria)
        {
            try
            {
                if (searchCriteria is null)
                {
                    throw new InvalidOperationException(nameof(searchCriteria));
                }

                var (valid, errorMessage) = SearchCriteriaValidation.Validate(searchCriteria);
                if (!valid)
                {
                    return BadRequest(errorMessage);
                }

                var searchResults = await _archiveSearchRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

                return searchResults.Result.Count > 0 ? Ok(searchResults) : NoContent();
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                _logger.LogError(e, "An error occurred while processing request");
                _logger.LogError(e.ToString());
                return new ObjectResult("An error occured while processing the request.")
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                };
            }
        }
    }
}
