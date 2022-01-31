using System;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.EntryPoint.Models;

namespace Energinet.DataHub.MessageArchive.EntryPoint.BlobServices
{
    /// <summary>
    /// Blob archive abstraction
    /// </summary>
    public interface IBlobArchive
    {
        /// <summary>
        /// Method for moving blob
        /// </summary>
        /// <param name="itemToMove"></param>
        /// <returns>Uri to new blob</returns>
        Task<Uri> MoveToArchiveAsync(BlobItemData itemToMove);
    }
}
