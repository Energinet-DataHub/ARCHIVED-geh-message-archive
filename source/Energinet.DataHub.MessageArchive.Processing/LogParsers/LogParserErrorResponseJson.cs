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
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing.LogParsers.ErrorParsers;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public class LogParserErrorResponseJson : LogParserBlobProperties
    {
        public override async Task<BaseParsedModel> ParseAsync(BlobItemData blobItemData)
        {
            ArgumentNullException.ThrowIfNull(blobItemData, nameof(blobItemData));

            var parsedModel = await base.ParseAsync(blobItemData).ConfigureAwait(false);

            // Not expecting a long string so we read the entire error message
            using var reader = new StreamReader(blobItemData.ContentStream);
            var jsonContentString = await reader.ReadToEndAsync().ConfigureAwait(false);

            parsedModel.Errors = JsonErrorParser.ParseErrors(jsonContentString);
            return parsedModel;
        }
    }
}
