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

namespace Energinet.DataHub.MessageArchive.EntryPoint.LogParsers.Utilities
{
    public static class ElementNames
    {
        public const string MRid = "mRID";
        public const string Series = "Series";
        public const string Type = "type";
        public const string ProcessProcessType = "process.processType";
        public const string BusinessSectorType = "businessSector.type";
        public const string ReasonCode = "reason.code";
        public const string SenderMarketParticipantmRid = "sender_MarketParticipant.mRID";
        public const string SenderMarketParticipantmarketRoletype = "sender_MarketParticipant.marketRole.type";
        public const string ReceiverMarketParticipantmRid = "receiver_MarketParticipant.mRID";
        public const string ReceiverMarketParticipantmarketRoletype = "receiver_MarketParticipant.marketRole.type";
        public const string CreatedDateTime = "createdDateTime";
        public const string OriginalTransactionIdReferenceSeriesmRid = "originalTransactionIDReference_Series.mRID";
        public const string OriginalTransactionIdReferenceMktActivityRecordmRid = "originalTransactionIDReference_MktActivityRecord.mRID";
        public const string RegistrationDateAndOrTimedateTime = "registration_DateAndOrTime.dateTime";
        public const string InDomainmRid = "in_Domain.mRID";
        public const string OutDomainmRid = "out_Domain.mRID";
        public const string Product = "product";
        public const string MeasureUnitname = "measure_Unit.name";
        public const string Period = "Period";
        public const string Resolution = "resolution";
        public const string TimeInterval = "timeInterval";
        public const string Point = "Point";
        public const string MarketEvaluationPointmRid = "marketEvaluationPoint.mRID";
        public const string MarketEvaluationPointtype = "marketEvaluationPoint.type";
        public const string Start = "start";
        public const string End = "end";
        public const string Position = "position";
        public const string Quantity = "quantity";
        public const string Quality = "quality";
        public const string CodingScheme = "codingScheme";
    }
}
