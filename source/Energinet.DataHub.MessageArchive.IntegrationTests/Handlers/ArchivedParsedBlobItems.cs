using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.PersistenceModels;
using Energinet.DataHub.MessageArchive.Processing;

namespace Energinet.DataHub.MessageArchive.IntegrationTests.Handlers;

public class ArchivedParsedBlobItems : IStorageWriter<CosmosRequestResponseLog>
{
#pragma warning disable CA1002
    public List<CosmosRequestResponseLog> ParsedLogs { get; } = new();
#pragma warning restore CA1002

    public Task WriteAsync(CosmosRequestResponseLog objectToSave)
    {
        ArgumentNullException.ThrowIfNull(objectToSave);
        ParsedLogs.Add(objectToSave);
        return Task.CompletedTask;
    }
}
