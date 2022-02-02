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
using System.Text.Json;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.ErrorParsers
{
    public static class JsonErrorParser
    {
        public static IEnumerable<ParsedErrorModel>? ParseErrors(string jsonString)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(jsonString);
                var errorPropParsed = jsonDocument.RootElement.TryGetProperty("error", out var errorProp);
                var code = errorProp.GetProperty("code").GetString();
                var message = errorProp.GetProperty("message").GetString();
                if (errorPropParsed)
                {
                    return new List<ParsedErrorModel>() { new (code ?? string.Empty, message ?? string.Empty) };
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
