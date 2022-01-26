using Energinet.DataHub.MessageArchive.EntryPoint.Models;
using Energinet.DataHub.MessageArchive.Utilities;

namespace Energinet.DataHub.MessageArchive.EntryPoint.Mappers
{
    public static class CosmosRequestResponseLogMapper
    {
        public static CosmosRequestResponseLog ToCosmosRequestResponseLog(BaseParsedModel fromobj)
        {
            Guard.ThrowIfNull(fromobj, nameof(fromobj));

            var toobj = new CosmosRequestResponseLog();
            toobj.MessageId = fromobj.MessageId;
            toobj.MessageType = fromobj.MessageType;
            toobj.ProcessType = fromobj.ProcessType;
            toobj.BusinessSectorType = fromobj.BusinessSectorType;
            toobj.CreatedDate = fromobj.CreatedDate;
            toobj.LogCreatedDate = fromobj.LogCreatedDate;
            toobj.SenderGln = fromobj.SenderGln;
            toobj.SenderGlnMarketRoleType = fromobj.SenderGlnMarketRoleType;
            toobj.ReceiverGln = fromobj.ReceiverGln;
            toobj.ReceiverGlnMarketRoleType = fromobj.ReceiverGlnMarketRoleType;
            toobj.BlobContentUri = fromobj.BlobContentUri;
            toobj.HttpData = fromobj.HttpData;
            toobj.InvocationId = fromobj.InvocationId;
            toobj.FunctionName = fromobj.FunctionName;
            toobj.TraceId = fromobj.TraceId;
            toobj.TraceParent = fromobj.TraceParent;

            return toobj;
        }
    }
}
