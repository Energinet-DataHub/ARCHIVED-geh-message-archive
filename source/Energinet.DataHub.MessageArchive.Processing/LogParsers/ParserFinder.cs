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
using System.Linq;
using System.Net;
using Energinet.DataHub.MessageArchive.Processing.LogParsers.Utilities;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MessageArchive.Processing.LogParsers
{
    public static class ParserFinder
    {
        public static ILogParser FindParser(
            BlobItemData blobItemData,
            ILogger<LogParserBlobProperties> logger)
        {
            ArgumentNullException.ThrowIfNull(blobItemData, nameof(blobItemData));

            if (IsErrorServerResponse(blobItemData.HttpStatusCode))
            {
                if (IsErrorXmlWithContent(blobItemData))
                {
                    return new LogParserErrorResponseXml();
                }

                if (IsJsonContentWithContent(blobItemData))
                {
                    return new LogParserErrorResponseJson();
                }

                return new LogParserBlobProperties();
            }

            if (IsEbixWithContent(blobItemData))
            {
                return new LogParserEbix(logger);
            }

            if (IsCimXmlWithContent(blobItemData))
            {
                return new LogParserXml(logger);
            }

            if (IsJsonContentWithContent(blobItemData))
            {
                return new LogParserJson(logger);
            }

            return new LogParserBlobProperties();
        }

        private static bool IsEbixWithContent(BlobItemData blobItemData)
        {
            return blobItemData.ContentLength > 0 && blobItemData.ContentType.Contains("ebix", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsCimXmlWithContent(BlobItemData blobItemData)
        {
            return blobItemData.ContentLength > 0 && blobItemData.ContentType.Contains("xml", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsErrorXmlWithContent(BlobItemData blobItemData)
        {
            return blobItemData.ContentLength > 0 && blobItemData.ContentType.Contains("xml", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsJsonContentWithContent(BlobItemData blobItemData)
        {
            return blobItemData.ContentLength > 0 && blobItemData.ContentType.Contains("json", StringComparison.InvariantCultureIgnoreCase);
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
