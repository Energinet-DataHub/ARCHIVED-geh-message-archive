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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MessageArchive.EntryPoint
{
    internal static class ApplicationInsightTelemetry
    {
        public static void SetupApplicationInsightTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var appInsightsInstrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"] ?? string.Empty;

            var appInsightsServiceOptions = new Microsoft.ApplicationInsights.WorkerService.ApplicationInsightsServiceOptions
            {
                InstrumentationKey = appInsightsInstrumentationKey,
                EnableDependencyTrackingTelemetryModule = !string.IsNullOrWhiteSpace(appInsightsInstrumentationKey),
            };

            services.AddApplicationInsightsTelemetryWorkerService(appInsightsServiceOptions);
        }
    }
}
