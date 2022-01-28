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
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers
{
    public static class ParserFinder
    {
        public static ILogParser? FindParser(string contentType, string content)
        {
            Guard.ThrowIfNull(contentType, nameof(contentType));
            Guard.ThrowIfNull(contentType, nameof(contentType));

            if ((contentType.Contains("xml") && !string.IsNullOrWhiteSpace(content))
                || (!string.IsNullOrWhiteSpace(content) && content.Trim().StartsWith("<?xml version", StringComparison.InvariantCulture))
                || (!string.IsNullOrWhiteSpace(content) && content.Trim().StartsWith("<cim:", StringComparison.InvariantCulture)))
            {
                return new LogParserXml();
            }

            if (content.Contains("json")
                || (!string.IsNullOrWhiteSpace(content) && content.Trim().StartsWith("{", StringComparison.InvariantCulture)))
            {
                return new LogParserJson();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new LogParserNoContent();
            }

            return null;
        }
    }
}
