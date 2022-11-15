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

namespace Energinet.DataHub.MessageArchive.Processing.Models
{
    public class BlobItemData
    {
        public BlobItemData(
            string name,
            IDictionary<string, string> metaData,
            IDictionary<string, string> indexTags,
            DateTimeOffset? blobCreatedOn,
            Uri uri)
        {
            Name = name;
            MetaData = metaData;
            IndexTags = indexTags;
            Uri = uri;
            BlobCreatedOn = blobCreatedOn;

            ContentStream = Stream.Null;
            ContentLength = 0;
        }

        public string Name { get; }

        public IDictionary<string, string> MetaData { get; }

        public IDictionary<string, string> IndexTags { get; }

        public Stream ContentStream { get; set; }

        public long ContentLength { get; set; }

        public DateTimeOffset? BlobCreatedOn { get; }

        public Uri Uri { get; }
    }
}
