using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedPackets.Models
{
    public class PacketsModel
    {
        [BsonId]
        [JsonConverter(typeof(Tools.Json.ObjectIdConverter))]
        public ObjectId PacketsID { get; set; }
        /// <summary>
        /// 总金额
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// 参与人数
        /// </summary>
        public int PeopleNum { get; set; }
        /// <summary>
        /// 创建红包账户ID
        /// </summary>
        public Participant CreatePeople { get; set; }
        /// <summary>
        /// 参与者
        /// </summary>
        public List<Participant> Participants { get; set; }
        /// <summary>
        /// 是否有效
        /// </summary>
        [BsonIgnore]
        public bool IsValid
        {
            get
            {
                return Participants == null || PeopleNum > Participants.Count;
            }
        }
        [BsonIgnore]
        public bool CurrentAccountOpened { get; set; }
        [JsonConverter(typeof(Tools.Json.DateConverterEndMinute))]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; }
        public string uniacid { get; set; }

        public void CheckCurrentAccountOpened(ObjectId objectId)
        {
            if (Participants == null)
            {
                CurrentAccountOpened = false;
                return;
            }
            CurrentAccountOpened = Participants.Exists(x => x.AccountID.Equals(objectId));
        }
        [BsonIgnore]
        public string OpenStatus
        {
            get
            {
                var opened = Participants == null ? 0 : Participants.Count;
                return $"{opened}/{PeopleNum}";
            }
        }
        [BsonIgnore]
        public int Top { get; set; }
    }

    public class VoicePacketsModel : PacketsModel
    {
        public string TextCmd { get; set; }

    }

    public enum PacketsType
    {
        /// <summary>
        /// 语音口令红包
        /// </summary>
        Voice = 0
    }

    public class Participant
    {
        [BsonId]
        [JsonConverter(typeof(Tools.Json.ObjectIdConverter))]
        public ObjectId AccountID { get; set; }
        public string AccountName { get; set; }
        public string AccountAvatar { get; set; }
        public decimal MoneyGet { get; set; }
        public string VoiceFileName { get; set; }

        [JsonConverter(typeof(Tools.Json.DateConverterEndMinute))]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; }
    }
}
