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
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Persistence;
using Energinet.DataHub.MessageArchive.Persistence.Containers;
using Energinet.DataHub.MessageArchive.Reader;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Tests.Repositories
{
    [UnitTest]
    public sealed class ArchiveReaderRepositoryTests
    {
        [Fact]
        public async Task GetSearchResultsAsync_SearchCriteriaIsNull_ThrowsException()
        {
            // Arrange
            var target = CreateTarget();

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.GetSearchResultsAsync(null!))
                .ConfigureAwait(false);
        }

        private static ArchiveSearchRepository CreateTarget()
        {
            var container = new Mock<IArchiveContainer>();

            return new (container.Object);
        }
    }
}
