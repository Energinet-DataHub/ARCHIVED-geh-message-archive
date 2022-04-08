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
using Energinet.DataHub.MessageArchive.Reader.Models;
using Energinet.DataHub.MessageArchive.Reader.Validation;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Reader.Handlers
{
    public class ArchiveSearchHandler : IArchiveSearchHandler
    {
        private readonly ILogger<ArchiveSearchHandler> _logger;
        private readonly IArchiveSearchRepository _archiveSearchRepository;

        public ArchiveSearchHandler(
            ILogger<ArchiveSearchHandler> logger,
            IArchiveSearchRepository archiveSearchRepository)
        {
            _logger = logger;
            _archiveSearchRepository = archiveSearchRepository;
        }

        public async Task<(SearchResults SearchResult, SearchCriteriaValidationResult ValidationResult)> SearchAsync(SearchCriteria searchCriteria)
        {
            var validationResult = SearchCriteriaValidation.Validate(searchCriteria);
            if (!validationResult.Valid)
            {
                _logger.LogInformation($"SearchCriteria Invalid, {validationResult.ErrorMessage}");
                return (new SearchResults(), validationResult);
            }

            var searchResults = await _archiveSearchRepository.GetSearchResultsAsync(searchCriteria).ConfigureAwait(false);

            return (searchResults, validationResult);
        }
    }
}
