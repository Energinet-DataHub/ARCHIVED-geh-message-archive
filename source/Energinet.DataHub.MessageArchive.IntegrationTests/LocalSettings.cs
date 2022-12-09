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
                    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(localSettingsPath))!;
                    if (json.TryGetValue("connectionString", out var cosmosConnectionString))
                    {
                        CosmosConnectionString = cosmosConnectionString;
                    }

                    if (json.TryGetValue("databaseName", out var databaseName))
                    {
                        CosmosDatabaseName = databaseName;
                    }

                    if (json.TryGetValue("disableAzurite", out var disableAzuriteStr) &&
                        bool.TryParse(disableAzuriteStr, out var disableAzurite))
                    {
                        DisableAzurite = disableAzurite;
                    }
                }
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignore
            }

            Environment.SetEnvironmentVariable("COSMOS_MESSAGE_ARCHIVE_CONNECTION_STRING", CosmosConnectionString);

            Environment.SetEnvironmentVariable("STORAGE_MESSAGE_ARCHIVE_CONNECTION_STRING", StorageAccountConnectionString);
            Environment.SetEnvironmentVariable("STORAGE_MESSAGE_ARCHIVE_CONTAINER_NAME", MessageArchiveContainerName);
            Environment.SetEnvironmentVariable("STORAGE_MESSAGE_ARCHIVE_PROCESSED_CONTAINER_NAME", MessageArchiveProcessedContainerName);
            Environment.SetEnvironmentVariable("EXTERNAL_OPEN_ID_URL", ExternalOpenIdUrl);
            Environment.SetEnvironmentVariable("INTERNAL_OPEN_ID_URL", InternalOpenIdUrl);
            Environment.SetEnvironmentVariable("BACKEND_SERVICE_APP_ID", BackendServiceAppId);
        }

        internal static string CosmosConnectionString { get; } = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        internal static string StorageAccountConnectionString { get; } = "UseDevelopmentStorage=true;";

        internal static string CosmosDatabaseName { get; } = "message-archive";

        internal static string MessageArchiveContainerName { get; } = "marketoplog";

        internal static string MessageArchiveProcessedContainerName { get; } = "marketoplogs-archive";

        internal static string ExternalOpenIdUrl { get; } = "https://dev002DataHubB2C.b2clogin.com/dev002DataHubB2C.onmicrosoft.com/B2C_1_sign_in_experiments/v2.0/.well-known/openid-configuration";

        internal static string InternalOpenIdUrl { get; } = "http://localhost:6000/.well-known/openid-configuration";

        internal static string BackendServiceAppId { get; } = "88e5d356-0c71-49e9-b260-d0629f3c0445";

        internal static bool DisableAzurite { get; }
    }
}
