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
using System.Text.Json;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities
{
    internal static class ParseTags
    {
        public static IDictionary<string, string>? ParseIndexTagsElement(BlobItemData blobItemData)
        {
            var parsedDictionary = ParseItemTags(blobItemData, "indextags");

            if (parsedDictionary.Count > 0)
            {
                return parsedDictionary;
            }

            return blobItemData.IndexTags.Any() ? blobItemData.IndexTags : null;
        }

        public static IDictionary<string, string>? ParseQueryTagsElement(BlobItemData blobItemData)
        {
            var parsedDictionary = ParseItemTags(blobItemData, "querytags");

            return parsedDictionary.Count > 0 ? parsedDictionary : null;
        }

        private static IDictionary<string, string> ParseItemTags(BlobItemData blobItemData, string tagName)
        {
            var tagsString = blobItemData.MetaData.TryGetValue(tagName, out var tags) ? tags : string.Empty;
            var tagsParsed = TryDeserializeTags(tagsString, out var dictionaryResult);
            if (tagsParsed && dictionaryResult.Count > 0)
            {
                return dictionaryResult;
            }

            return new Dictionary<string, string>();
        }

        private static bool TryDeserializeTags(string json, out IDictionary<string, string> result)
        {
            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                result = dictionary ?? new Dictionary<string, string>();
                return true;
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                result = new Dictionary<string, string>();
                return false;
            }
        }
    }
}
