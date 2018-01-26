using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedPackets.Models
{
    public class TransactionHistoryViewModel
    {
        public string StatementName { get; set; }
        [JsonConverter(typeof(Tools.Json.DateConverterEndMinute))]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; }
        public decimal RMB { get; set; }
        public string AccountName { get; set; }
        public Tools.Models.Gender Gender { get; set; }

        public string AccountAvatar { get; set; }
    }
}
