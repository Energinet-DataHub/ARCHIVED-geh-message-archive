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

            var asLinq = _archiveContainer.Container.GetItemLinqQueryable<CosmosRequestResponseLog>();
            var query = from searchResult in asLinq
                where (criteria.MessageId == null || criteria.MessageId == searchResult.MessageId) &&
                    (criteria.MessageType == null || criteria.MessageType == searchResult.MessageType) &&
                    (criteria.ProcessType == null || criteria.ProcessType == searchResult.ProcessType) &&
                    (criteria.SenderId == null || criteria.SenderId == searchResult.SenderGln) &&
                    (criteria.ReceiverId == null || criteria.ReceiverId == searchResult.ReceiverGln) &&
                    (criteria.SenderRoleType == null || criteria.SenderRoleType == searchResult.SenderGlnMarketRoleType) &&
                    (criteria.ReceiverRoleType == null || criteria.ReceiverRoleType == searchResult.ReceiverGlnMarketRoleType) &&
                    (criteria.DateTimeFrom == null || criteria.DateTimeFromParsed <= searchResult.LogCreatedDate) &&
                    (criteria.DateTimeTo == null || criteria.DateTimeToParsed >= searchResult.LogCreatedDate) &&
                    (criteria.InvocationId == null || criteria.InvocationId == searchResult.InvocationId) &&
                    (criteria.FunctionName == null || criteria.FunctionName == searchResult.FunctionName) &&
                    (criteria.TraceId == null || criteria.TraceId == searchResult.TraceId) &&
                    (criteria.BusinessSectorType == null || criteria.BusinessSectorType == searchResult.BusinessSectorType) &&
                    (criteria.ReasonCode == null || criteria.ReasonCode == searchResult.ReasonCode) &&
                    (criteria.ReferenceId == null || criteria.ReferenceId == searchResult.OriginalTransactionIDReferenceId)
                select searchResult;

            var cosmosDocuments = await ExecuteQueryAsync(query).ConfigureAwait(false);

            return Map(cosmosDocuments);
        }

        private static SearchResults Map(IEnumerable<CosmosRequestResponseLog> cosmosDocuments)
        {
            SearchResults searchResults = new ();

            foreach (var cosmosSearchResult in cosmosDocuments)
            {
                var searchResult =
                    Mappers.CosmosRequestResponseLogMapper.ToBaseParsedModels(cosmosSearchResult);
                searchResults.Result.Add(searchResult);
            }

            return searchResults;
        }

        private static async Task<List<CosmosRequestResponseLog>> ExecuteQueryAsync(IQueryable<CosmosRequestResponseLog> query)
        {
            List<CosmosRequestResponseLog> cosmosDocuments = new ();

            using var iterator = query.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                cosmosDocuments.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return cosmosDocuments;
        }
    }
}
