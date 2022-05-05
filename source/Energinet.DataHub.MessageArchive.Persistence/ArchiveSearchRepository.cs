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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Reader;
using Energinet.DataHub.MessageArchive.Reader.Models;
using Energinet.DataHub.MessageArchive.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Energinet.DataHub.MessageArchive.Persistence
{
    public class ArchiveSearchRepository : IArchiveSearchRepository
    {
        private readonly IArchiveContainer _archiveContainer;

        public ArchiveSearchRepository(IArchiveContainer archiveContainer)
        {
            _archiveContainer = archiveContainer;
        }

        public async Task<SearchResults> GetSearchResultsAsync(SearchCriteria criteria)
        {
            Guard.ThrowIfNull(criteria, nameof(criteria));

            var asLinq = _archiveContainer.Container
                .GetItemLinqQueryable<CosmosRequestResponseLog>(
                    requestOptions: new QueryRequestOptions() { MaxItemCount = criteria.MaxItemCount },
                    continuationToken: criteria.ContinuationToken);

            var ignoreProcessTypes = criteria.ProcessTypes is not { Count: > 0 };
            var ignoreRsmNames = criteria.RsmNames is not { Count: > 0 };
            var ignoreBodyRequirement = criteria.IncludeResultsWithoutContent || !string.IsNullOrWhiteSpace(criteria.TraceId);

            var query = from searchResult in asLinq
                where (criteria.MessageId == null || criteria.MessageId == searchResult.MessageId) &&
                    (criteria.MessageType == null || criteria.MessageType == searchResult.MessageType) &&
                    (criteria.SenderId == null || criteria.SenderId == searchResult.SenderGln) &&
                    (criteria.ReceiverId == null || criteria.ReceiverId == searchResult.ReceiverGln) &&
                    (criteria.SenderRoleType == null || criteria.SenderRoleType == searchResult.SenderGlnMarketRoleType) &&
                    (criteria.ReceiverRoleType == null || criteria.ReceiverRoleType == searchResult.ReceiverGlnMarketRoleType) &&
                    (criteria.DateTimeFrom == null || criteria.DateTimeFromParsed <= searchResult.CreatedDate) &&
                    (criteria.DateTimeTo == null || criteria.DateTimeToParsed >= searchResult.CreatedDate) &&
                    (criteria.InvocationId == null || criteria.InvocationId == searchResult.InvocationId) &&
                    (criteria.FunctionName == null || criteria.FunctionName == searchResult.FunctionName) &&
                    (criteria.TraceId == null || criteria.TraceId == searchResult.TraceId) &&
                    (criteria.BusinessSectorType == null || criteria.BusinessSectorType == searchResult.BusinessSectorType) &&
                    (criteria.ReasonCode == null || criteria.ReasonCode == searchResult.ReasonCode) &&
                    (ignoreBodyRequirement || searchResult.HaveBodyContent == true) &&

                    (ignoreProcessTypes || (criteria.ProcessTypes != null && searchResult.ProcessType != null && criteria.ProcessTypes.Contains(searchResult.ProcessType))) &&
                    (ignoreRsmNames || (criteria.RsmNames != null && searchResult.RsmName != null && criteria.RsmNames.Contains(searchResult.RsmName)))
                select searchResult;

            var (cosmosDocuments, continuationToken) = await ExecuteQueryWithContinuationTokenAsync(query).ConfigureAwait(false);

            await AddRelatedMessagesIfAnyAsync(criteria, cosmosDocuments).ConfigureAwait(false);

            var searchResultMapped = Map(cosmosDocuments);
            searchResultMapped.ContinuationToken = continuationToken;
            return searchResultMapped;
        }

        private static SearchResults Map(IEnumerable<CosmosRequestResponseLog> cosmosDocuments)
        {
            SearchResults searchResults = new();

            foreach (var cosmosSearchResult in cosmosDocuments)
            {
                var searchResult = Reader.Mappers.CosmosRequestResponseLogMapper.ToBaseParsedModels(cosmosSearchResult);
                searchResults.Result.Add(searchResult);
            }

            return searchResults;
        }

        private static async Task<List<CosmosRequestResponseLog>> ExecuteQueryAsync(IQueryable<CosmosRequestResponseLog> query)
        {
            List<CosmosRequestResponseLog> cosmosDocuments = new();

            using var iterator = query.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                cosmosDocuments.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            return cosmosDocuments;
        }

        private static async Task<(List<CosmosRequestResponseLog> Result, string? ContinuationToken)> ExecuteQueryWithContinuationTokenAsync(IQueryable<CosmosRequestResponseLog> query)
        {
            List<CosmosRequestResponseLog> cosmosDocuments = new();

            using var iterator = query.ToFeedIterator();

            var response = await iterator.ReadNextAsync().ConfigureAwait(false);
            cosmosDocuments.AddRange(response);

            return (cosmosDocuments, response.ContinuationToken);
        }

        private async Task AddRelatedMessagesIfAnyAsync(SearchCriteria criteria, List<CosmosRequestResponseLog> documents)
        {
            if (criteria.MessageId != null && criteria.IncludeRelated == true && documents.Any())
            {
                var document = documents.FirstOrDefault();
                var httpDataType = document?.HttpData ?? "unknown";

                if (httpDataType.Equals("request", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionRecords = document?.TransactionRecords ?? Array.Empty<TransactionRecord>();
                    var transactionRecordIds = transactionRecords.Select(t => t.MRid).ToArray();

                    var asLinqIn = _archiveContainer.Container.GetItemLinqQueryable<CosmosRequestResponseLog>();
                    var relatedQuery = from relatedMessageResult in asLinqIn
                        where relatedMessageResult.HttpData == "response" &&
                              transactionRecordIds != null &&
                              relatedMessageResult.TransactionRecords != null &&
                              relatedMessageResult.TransactionRecords.Any(x => transactionRecordIds.Contains(x.OriginalTransactionIdReferenceId))
                        select relatedMessageResult;

                    var relatedCosmosDocuments = await ExecuteQueryAsync(relatedQuery).ConfigureAwait(false);
                    documents.AddRange(relatedCosmosDocuments);
                }
                else if (httpDataType.Equals("response", StringComparison.OrdinalIgnoreCase))
                {
                    var transactionRecords = document?.TransactionRecords ?? Array.Empty<TransactionRecord>();
                    var originalReferenceIds = transactionRecords.Select(t => t.OriginalTransactionIdReferenceId).ToArray();

                    var asLinqIn = _archiveContainer.Container.GetItemLinqQueryable<CosmosRequestResponseLog>();
                    var relatedQuery = from relatedMessageResult in asLinqIn
                        where relatedMessageResult.HttpData == "request" &&
                              originalReferenceIds != null &&
                              relatedMessageResult.TransactionRecords != null &&
                              relatedMessageResult.TransactionRecords.Any(x => originalReferenceIds.Contains(x.MRid))
                        select relatedMessageResult;

                    var relatedCosmosDocuments = await ExecuteQueryAsync(relatedQuery).ConfigureAwait(false);
                    documents.AddRange(relatedCosmosDocuments);
                }
            }
        }
    }
}
