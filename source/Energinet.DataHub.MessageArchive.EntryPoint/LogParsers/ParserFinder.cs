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
using System.Net;
using System.Xml.Linq;
using Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.Utilities;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers
{
    public static class ParserFinder
    {
        public static ILogParser FindParser(string contentType, string httpStatusCode, string content)
        {
            Guard.ThrowIfNull(contentType, nameof(contentType));

            if (IsErrorServerResponse(httpStatusCode))
            {
                if (IsErrorXmlWithContent(contentType, content))
                {
                    return new LogParserErrorResponseXml();
                }

                if (IsErrorJsonWithContent(contentType, content))
                {
                    return new LogParserErrorResponseJson();
                }

                return new LogParserBlobProperties();
            }

            if (IsXmlWithContent(contentType, content))
            {
                return new LogParserXml();
            }

            if (IsJsonContent(contentType, content))
            {
                return new LogParserJson();
            }

            return new LogParserBlobProperties();
        }

        private static bool IsXmlWithContent(string contentType, string content)
        {
            return (contentType.Contains("xml") && !string.IsNullOrWhiteSpace(content))
                || (!string.IsNullOrWhiteSpace(content) &&
                    content.Trim().StartsWith("<?xml version", StringComparison.InvariantCulture))
                || (!string.IsNullOrWhiteSpace(content) &&
                    content.Trim().StartsWith("<cim:", StringComparison.InvariantCulture));
        }

        private static bool IsErrorXmlWithContent(string contentType, string content)
        {
            return contentType.Contains("xml")
                   && !string.IsNullOrWhiteSpace(content)
                   && content.Trim().Contains("<Error>", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsJsonContent(string contentType, string content)
        {
            return contentType.Contains("json")
                   || (!string.IsNullOrWhiteSpace(content) &&
                       content.Trim().StartsWith("{", StringComparison.InvariantCulture));
        }

        private static bool IsErrorJsonWithContent(string contentType, string content)
        {
            return contentType.Contains("json")
                   || (!string.IsNullOrWhiteSpace(content)
                       && content.Contains("error", StringComparison.InvariantCultureIgnoreCase)
                       && content.Contains("code", StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool IsErrorServerResponse(string httpStatusCodeStr)
        {
            if (!string.IsNullOrWhiteSpace(httpStatusCodeStr))
            {
                var statusCodeParsed = Enum.TryParse<HttpStatusCode>(httpStatusCodeStr, out var httpStatusCode);
                return statusCodeParsed && HttpErrorStatusCodes.StatusCodes.Contains(httpStatusCode);
            }

            return false;
        }
    }
}
