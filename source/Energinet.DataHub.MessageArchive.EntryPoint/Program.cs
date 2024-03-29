﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Common.SimpleInjector;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace Energinet.DataHub.MessageArchive.EntryPoint
{
    public static class Program
    {
        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Issue: https://github.com/dotnet/roslyn-analyzers/issues/5712")]
        public static async Task Main()
        {
            await using var startup = new Startup();
            await using (startup.ConfigureAwait(false))
            {
                var host = new HostBuilder()
                    .ConfigureFunctionsWorkerDefaults(options =>
                    {
                        options.UseMiddleware<SimpleInjectorScopedRequest>();
                    })
                    .ConfigureServices(startup.ConfigureServices)
                    .Build()
                    .UseSimpleInjector(startup.Container);

                await host.RunAsync().ConfigureAwait(false);
            }
        }
    }
}
