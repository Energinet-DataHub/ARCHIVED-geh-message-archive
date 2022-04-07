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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MessageArchive.Client.Abstractions.Models;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MessageArchive.Client.Tests
{
    [UnitTest]
    public class MessageArchiveClientTests
    {
        [Fact]
        public async Task Test_MessageArchiveClient_Search()
        {
            // Arrange
            using var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Version = HttpVersion.Version20,
                Content = new StringContent(JsonResult),
                ReasonPhrase = null,
                RequestMessage = null,
            };

            var handlerMock = GetHandlerMock(httpResponseMessage);

            using var httpClient = new HttpClient(handlerMock.Object);
            httpClient.BaseAddress = new Uri("https://localhost/messagearchive/api");

            var messageArchiveClient = new MessageArchiveClient(httpClient);

            var searchCriteria = new MessageArchiveSearchCriteria();
            searchCriteria.MessageId = "1234";

            // Act
            var result = await messageArchiveClient
                .SearchLogsAsync(searchCriteria)
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.Result.Count == 5);
        }

        [Fact]
        public async Task Test_MessageArchiveClient_Search_Unauthorized()
        {
            // Arrange
            using var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Version = HttpVersion.Version20,
                Content = new StringContent(string.Empty),
                ReasonPhrase = null,
                RequestMessage = null,
            };

            var handlerMock = GetHandlerMock(httpResponseMessage);

            using var httpClient = new HttpClient(handlerMock.Object);
            httpClient.BaseAddress = new Uri("https://localhost/messagearchive/api");

            var messageArchiveClient = new MessageArchiveClient(httpClient);

            var searchCriteria = new MessageArchiveSearchCriteria();
            searchCriteria.MessageId = "1234";

            // Act
            var result = messageArchiveClient
                .SearchLogsAsync(searchCriteria);

            // Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => result)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Test_MessageArchiveClient_Search_NullResult()
        {
            // Arrange
            using var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Version = HttpVersion.Version20,
                Content = new StringContent(string.Empty),
                ReasonPhrase = null,
                RequestMessage = null,
            };

            var handlerMock = GetHandlerMock(httpResponseMessage);

            using var httpClient = new HttpClient(handlerMock.Object);
            httpClient.BaseAddress = new Uri("https://localhost/messagearchive/api");

            var messageArchiveClient = new MessageArchiveClient(httpClient);

            // Act
            var result = await
                messageArchiveClient
                .SearchLogsAsync(new MessageArchiveSearchCriteria { MessageId = "1234" })
                .ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Test_MessageArchiveClient_ContentStreamRead()
        {
            // Arrange
            using var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Version = HttpVersion.Version20,
                Content = new StringContent("<cim>result</cim>"),
                ReasonPhrase = null,
                RequestMessage = null,
            };

            var handlerMock = GetHandlerMock(httpResponseMessage);

            using var httpClient = new HttpClient(handlerMock.Object);
            httpClient.BaseAddress = new Uri("https://localhost/messagearchive/api");

            var messageArchiveClient = new MessageArchiveClient(httpClient);

            // Act
            var result = await
                messageArchiveClient
                    .GetStreamFromStorageAsync("name")
                    .ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
        }

        private static Mock<HttpMessageHandler> GetHandlerMock(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            return handlerMock;
        }

#pragma warning disable SA1201
        private const string JsonResult =
#pragma warning restore SA1201
            "{\"result\":[{\"messageId\":\"05aa5560-d5b8-47cf-acac-6270abb02e2f\",\"messageType\":\"E59\",\"processType\":\"D15\",\"businessSectorType\":\"23\",\"reasonCode\":\"A01\",\"createdDate\":\"2022-03-28T13:58:32+00:00\",\"logCreatedDate\":\"2022-03-28T14:02:10+00:00\",\"senderGln\":\"5790001330552\",\"senderGlnMarketRoleType\":\"DDZ\",\"receiverGln\":\"5790000705184\",\"receiverGlnMarketRoleType\":\"DDM\",\"blobContentUri\":\"https://stmarketlogsharedresb002.blob.core.windows.net/marketoplogs-archive/2022-03-28/PeekMasterData_8e6124b1-36f0-43ac-b022-73d9c0fde334_af90b3af-426d-409e-ba29-d93828b5458c_00-2a8e34a7180710ad6ff47d5ae83736c0-0bcd20a84e797c4f-00_3cf3ed21-8cdd-4ac1-9181-eabc76ea27c9_2022-03-28T14-02-10Z_response.txt\",\"httpData\":\"response\",\"invocationId\":\"af90b3af-426d-409e-ba29-d93828b5458c\",\"functionName\":\"PeekMasterData\",\"traceId\":\"2a8e34a7180710ad6ff47d5ae83736c0\",\"traceParent\":\"00-2a8e34a7180710ad6ff47d5ae83736c0-0bcd20a84e797c4f-00\",\"responseStatus\":\"OK\",\"originalTransactionIDReferenceId\":\"SAP.KAM290-50000069397841\",\"rsmName\":\"confirmrequestchangeaccountingpointcharacteristics\",\"data\":{\"functionname\":\"PeekMasterData\",\"httpdatatype\":\"response\",\"invocationid\":\"af90b3af-426d-409e-ba29-d93828b5458c\",\"jwtactorid\":\"8e6124b1-36f0-43ac-b022-73d9c0fde334\",\"statuscode\":\"OK\",\"traceid\":\"2a8e34a7180710ad6ff47d5ae83736c0\",\"traceparent\":\"00-2a8e34a7180710ad6ff47d5ae83736c0-0bcd20a84e797c4f-00\"},\"errors\":null},{\"messageId\":\"ac59b70c-e7f2-488a-84fc-3a6b5caa9312\",\"messageType\":\"E59\",\"processType\":\"E02\",\"businessSectorType\":\"23\",\"reasonCode\":\"A01\",\"createdDate\":\"2022-03-28T14:01:38+00:00\",\"logCreatedDate\":\"2022-03-28T14:02:20+00:00\",\"senderGln\":\"5790001330552\",\"senderGlnMarketRoleType\":\"DDZ\",\"receiverGln\":\"5790000705184\",\"receiverGlnMarketRoleType\":\"DDM\",\"blobContentUri\":\"https://stmarketlogsharedresb002.blob.core.windows.net/marketoplogs-archive/2022-03-28/PeekMasterData_8e6124b1-36f0-43ac-b022-73d9c0fde334_ae3471de-abdc-4d8e-9f94-1465cfbcec58_00-c03d6af7e9fa3f50bca36da4ac8e724c-029c26f128f48a41-00_97ddef99-81d7-4eed-81b8-7d18b6b1cbab_2022-03-28T14-02-20Z_response.txt\",\"httpData\":\"response\",\"invocationId\":\"ae3471de-abdc-4d8e-9f94-1465cfbcec58\",\"functionName\":\"PeekMasterData\",\"traceId\":\"c03d6af7e9fa3f50bca36da4ac8e724c\",\"traceParent\":\"00-c03d6af7e9fa3f50bca36da4ac8e724c-029c26f128f48a41-00\",\"responseStatus\":\"OK\",\"originalTransactionIDReferenceId\":\"SAP.KAM290-50000069397843\",\"rsmName\":\"confirmrequestchangeaccountingpointcharacteristics\",\"data\":{\"functionname\":\"PeekMasterData\",\"httpdatatype\":\"response\",\"invocationid\":\"ae3471de-abdc-4d8e-9f94-1465cfbcec58\",\"jwtactorid\":\"8e6124b1-36f0-43ac-b022-73d9c0fde334\",\"statuscode\":\"OK\",\"traceid\":\"c03d6af7e9fa3f50bca36da4ac8e724c\",\"traceparent\":\"00-c03d6af7e9fa3f50bca36da4ac8e724c-029c26f128f48a41-00\"},\"errors\":null},{\"messageId\":\"53e63239-3191-471a-89b8-9c752fb2947f\",\"messageType\":\"E59\",\"processType\":\"D15\",\"businessSectorType\":\"23\",\"reasonCode\":\"A01\",\"createdDate\":\"2022-03-28T14:10:42+00:00\",\"logCreatedDate\":\"2022-03-28T14:12:40+00:00\",\"senderGln\":\"5790001330552\",\"senderGlnMarketRoleType\":\"DDZ\",\"receiverGln\":\"5790000705184\",\"receiverGlnMarketRoleType\":\"DDM\",\"blobContentUri\":\"https://stmarketlogsharedresb002.blob.core.windows.net/marketoplogs-archive/2022-03-28/PeekMasterData_8e6124b1-36f0-43ac-b022-73d9c0fde334_4a4829a5-5d8f-4388-a3c9-d6efc1ff7b13_00-e0edaf8ed0cfeb1e6084ec87745acf23-e254b7cd377f894f-00_ddb749f1-8c06-420d-9718-ab625cf3ea63_2022-03-28T14-12-40Z_response.txt\",\"httpData\":\"response\",\"invocationId\":\"4a4829a5-5d8f-4388-a3c9-d6efc1ff7b13\",\"functionName\":\"PeekMasterData\",\"traceId\":\"e0edaf8ed0cfeb1e6084ec87745acf23\",\"traceParent\":\"00-e0edaf8ed0cfeb1e6084ec87745acf23-e254b7cd377f894f-00\",\"responseStatus\":\"OK\",\"originalTransactionIDReferenceId\":\"SAP.KAM290-50000069397847\",\"rsmName\":\"confirmrequestchangeaccountingpointcharacteristics\",\"data\":{\"functionname\":\"PeekMasterData\",\"httpdatatype\":\"response\",\"invocationid\":\"4a4829a5-5d8f-4388-a3c9-d6efc1ff7b13\",\"jwtactorid\":\"8e6124b1-36f0-43ac-b022-73d9c0fde334\",\"statuscode\":\"OK\",\"traceid\":\"e0edaf8ed0cfeb1e6084ec87745acf23\",\"traceparent\":\"00-e0edaf8ed0cfeb1e6084ec87745acf23-e254b7cd377f894f-00\"},\"errors\":null},{\"messageId\":\"36fb3df1-5d59-42a2-a6da-ac5f31153d00\",\"messageType\":\"E59\",\"processType\":\"E79\",\"businessSectorType\":\"23\",\"reasonCode\":\"A01\",\"createdDate\":\"2022-03-28T14:20:30+00:00\",\"logCreatedDate\":\"2022-03-28T14:22:50+00:00\",\"senderGln\":\"5790001330552\",\"senderGlnMarketRoleType\":\"DDZ\",\"receiverGln\":\"5790000705184\",\"receiverGlnMarketRoleType\":\"DDM\",\"blobContentUri\":\"https://stmarketlogsharedresb002.blob.core.windows.net/marketoplogs-archive/2022-03-28/PeekMasterData_8e6124b1-36f0-43ac-b022-73d9c0fde334_0d440cba-302e-4032-9f1b-79767b071d02_00-574488152a4b1384da6acafc19af6029-a84fccfe495bc34d-00_4511b303-81e3-4871-a389-7f854f0b6825_2022-03-28T14-22-50Z_response.txt\",\"httpData\":\"response\",\"invocationId\":\"0d440cba-302e-4032-9f1b-79767b071d02\",\"functionName\":\"PeekMasterData\",\"traceId\":\"574488152a4b1384da6acafc19af6029\",\"traceParent\":\"00-574488152a4b1384da6acafc19af6029-a84fccfe495bc34d-00\",\"responseStatus\":\"OK\",\"originalTransactionIDReferenceId\":\"SAP.KAM290-50000069397849\",\"rsmName\":\"confirmrequestchangeaccountingpointcharacteristics\",\"data\":{\"functionname\":\"PeekMasterData\",\"httpdatatype\":\"response\",\"invocationid\":\"0d440cba-302e-4032-9f1b-79767b071d02\",\"jwtactorid\":\"8e6124b1-36f0-43ac-b022-73d9c0fde334\",\"statuscode\":\"OK\",\"traceid\":\"574488152a4b1384da6acafc19af6029\",\"traceparent\":\"00-574488152a4b1384da6acafc19af6029-a84fccfe495bc34d-00\"},\"errors\":null},{\"messageId\":\"2e7f1f4b-c046-4e0c-a8b6-5c1a3a90b813\",\"messageType\":\"E59\",\"processType\":\"E79\",\"businessSectorType\":\"23\",\"reasonCode\":\"A01\",\"createdDate\":\"2022-03-28T14:28:24+00:00\",\"logCreatedDate\":\"2022-03-28T14:33:00+00:00\",\"senderGln\":\"5790001330552\",\"senderGlnMarketRoleType\":\"DDZ\",\"receiverGln\":\"5790000705184\",\"receiverGlnMarketRoleType\":\"DDM\",\"blobContentUri\":\"https://stmarketlogsharedresb002.blob.core.windows.net/marketoplogs-archive/2022-03-28/PeekMasterData_8e6124b1-36f0-43ac-b022-73d9c0fde334_d773cf78-9fad-43ac-8f05-406dc4a70698_00-b93106814269ea91ee8d9de2b2ca5af3-7e319d230bb1944d-00_1f4f5277-c7c5-4ff2-aafd-bc275f8db372_2022-03-28T14-33-00Z_response.txt\",\"httpData\":\"response\",\"invocationId\":\"d773cf78-9fad-43ac-8f05-406dc4a70698\",\"functionName\":\"PeekMasterData\",\"traceId\":\"b93106814269ea91ee8d9de2b2ca5af3\",\"traceParent\":\"00-b93106814269ea91ee8d9de2b2ca5af3-7e319d230bb1944d-00\",\"responseStatus\":\"OK\",\"originalTransactionIDReferenceId\":\"SAP.KAM290-50000069397851\",\"rsmName\":\"confirmrequestchangeaccountingpointcharacteristics\",\"data\":{\"functionname\":\"PeekMasterData\",\"httpdatatype\":\"response\",\"invocationid\":\"d773cf78-9fad-43ac-8f05-406dc4a70698\",\"jwtactorid\":\"8e6124b1-36f0-43ac-b022-73d9c0fde334\",\"statuscode\":\"OK\",\"traceid\":\"b93106814269ea91ee8d9de2b2ca5af3\",\"traceparent\":\"00-b93106814269ea91ee8d9de2b2ca5af3-7e319d230bb1944d-00\"},\"errors\":null}],\"continuationToken\":null}";
    }
}
