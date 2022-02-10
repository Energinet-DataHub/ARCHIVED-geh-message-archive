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
using System.IO;
using System.Text.Json;

namespace Energinet.DataHub.MessageArchive.IntegrationTests
{
    internal static class LocalSettings
    {
        static LocalSettings()
        {
            try
            {
                var localSettingsPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "local.settings.json");

                if (File.Exists(localSettingsPath))
                {
                    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(localSettingsPath)) !;
                    if (json.TryGetValue("connectionString", out var cosmosConnectionString))
                    {
                        ConnectionString = cosmosConnectionString;
                    }

                    if (json.TryGetValue("databaseName", out var databaseName))
                    {
                        DatabaseName = databaseName;
                    }
                }
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignore
            }

            Environment.SetEnvironmentVariable("COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING", ConnectionString);
        }

        internal static string ConnectionString { get; } = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        internal static string DatabaseName { get; } = "Search";
    }
}
