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

using System;
using System.Collections.Generic;
using System.IO;
using Energinet.DataHub.MessageArchive.Processing.Models;
using Moq;

namespace Energinet.DataHub.MessageArchive.Tests
{
    public static class MockedTypes
    {
        public static BlobItemData BlobItemData(string contentType, string content, IDictionary<string, string>? indexTags = null)
        {
            var uri = new Uri("https://localhost/TestBlob");

            var contentStream = GenerateStreamFromString(content);

            return new BlobItemData(
                It.IsAny<string>(),
                new Dictionary<string, string>() { { "contenttype", contentType } },
                indexTags ?? new Dictionary<string, string>(),
                DateTimeOffset.Now,
                uri)
            {
                ContentStream = contentStream,
                ContentLength = content?.Length ?? 0,
            };
        }

        public static BlobItemData BlobItemDataStream(string contentType, Stream contentStream, IDictionary<string, string>? indexTags = null)
        {
            var uri = new Uri("https://localhost/TestBlob");

            var blobItem = new BlobItemData(
                It.IsAny<string>(),
                new Dictionary<string, string>() { { "contenttype", contentType } },
                indexTags ?? new Dictionary<string, string>(),
                DateTimeOffset.Now,
                uri);

            blobItem.ContentStream = contentStream;
            blobItem.ContentLength = contentStream?.Length ?? 0;
            return blobItem;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
