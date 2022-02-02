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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.Documents;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.EntryPoint.Repository.Containers;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Cosmos.Linq;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Repository
{
    public class ArchiveReaderRepository : IArchiveReaderRepository
    {
        private readonly IArchiveContainer _archiveContainer;

        public ArchiveReaderRepository(IArchiveContainer archiveContainer)
        {
            _archiveContainer = archiveContainer;
        }

        public async Task<SearchResults> GetSearchResultsAsync(SearchCriteria criteria)
        {
            Guard.ThrowIfNull(criteria, nameof(criteria));

            var asLinq = _archiveContainer.Container.GetItemLinqQueryable<CosmosSearchResult>();
            var query = from searchResult in asLinq
                where (criteria.MessageId == null || criteria.MessageId == searchResult.MessageId) &&
                      (criteria.MessageType == null || criteria.MessageType == searchResult.MessageType) &&
                      (criteria.ProcessId == null || criteria.ProcessId == searchResult.ProcessId) &&
                      (criteria.SenderId == null || criteria.SenderId == searchResult.SenderId) &&
                      (criteria.BusinessReasonCode == null || criteria.BusinessReasonCode == searchResult.BusinessReasonCode) &&
                      (criteria.DateTimeFrom == null || criteria.DateTimeFrom <= searchResult.DateTimeReceived) &&
                      (criteria.DateTimeTo == null || criteria.DateTimeTo >= searchResult.DateTimeReceived)
                select searchResult;

            List<CosmosSearchResult> cosmosDocuments = new ();

            using var iterator = query.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                cosmosDocuments.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return Map(cosmosDocuments);
        }

        private static SearchResults Map(IEnumerable<CosmosSearchResult> cosmosDocuments)
        {
            SearchResults searchResults = new ();

            foreach (var cosmosSearchResult in cosmosDocuments)
            {
                searchResults.Results.Add(new SearchResult(
                    cosmosSearchResult.Id,
                    cosmosSearchResult.MessageType,
                    cosmosSearchResult.ProcessId,
                    cosmosSearchResult.DateTimeReceived,
                    cosmosSearchResult.SenderId,
                    cosmosSearchResult.BusinessReasonCode));
            }

            return searchResults;
        }
    }
}
