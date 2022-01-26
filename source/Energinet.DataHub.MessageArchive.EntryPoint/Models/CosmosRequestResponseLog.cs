using System.ComponentModel;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Models
{
    public class CosmosRequestResponseLog : BaseParsedModel
    {
        public string Id { get; set; }

        public string PartitionKey { get; set; }
    }
}
