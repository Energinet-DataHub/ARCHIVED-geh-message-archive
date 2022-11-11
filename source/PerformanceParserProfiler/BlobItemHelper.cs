using System;
using System.Collections.Generic;
using System.IO;
using Energinet.DataHub.MessageArchive.Processing.Models;

namespace PerformanceParserProfiler;

public static class BlobItemHelper
{
    public static BlobItemData BlobItemDataStream(string contentType, Stream contentStream, IDictionary<string, string>? indexTags = null)
    {
        var uri = new Uri("https://localhost/TestBlob");

        var blobItem = new BlobItemData(
            "TestFile",
            new Dictionary<string, string>() { { "contenttype", contentType } },
            indexTags ?? new Dictionary<string, string>(),
            string.Empty,
            DateTimeOffset.Now,
            uri);

        blobItem.ContentStream = contentStream;
        blobItem.ContentLength = contentStream?.Length;
        return blobItem;
    }
}
