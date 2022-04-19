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
using System.Xml.Linq;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers.ErrorParsers
{
    public static class XmlErrorParser
    {
        public static IEnumerable<ParsedErrorModel> ParseErrors(XElement xmlDocument)
        {
            Guard.ThrowIfNull(xmlDocument, nameof(xmlDocument));

            var errors = xmlDocument.DescendantsAndSelf("Error");
            return errors
                .Select(e => new ParsedErrorModel(
                    e.DescendantsAndSelf("Code").FirstOrDefault()?.Value ?? "unknown",
                    e.DescendantsAndSelf("Message").FirstOrDefault()?.Value ?? "unknown"))
                .ToList();
        }
    }
}
