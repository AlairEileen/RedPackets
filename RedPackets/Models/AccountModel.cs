using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tools.Json;
using Tools.Models;

namespace RedPackets.Models
{
    public class AccountModel : BaseAccount
    {
        public string OpenID { get; set; }
        /// <summary>
        /// 账单
        /// </summary>
        public List<Statement> Statements { get; set; }

        public List<WeChatOrder> WeChatOrders { get; set; }
        /// <summary>
        /// 微擎专用
        /// </summary>
        public string uniacid { get; set; }
        /// <summary>
        /// 余额
        /// </summary>
        public decimal Balances { get; set; }
        /// <summary>
        /// 发送的语音口令红包
        /// </summary>
        public List<VoicePacketsModel> SendPackets { get; set; }
        /// <summary>
        /// 获得的语音口令红包
        /// </summary>
        public List<VoicePacketsModel> ReceivePackets { get; set; }
        /// <summary>
        /// 接收到的红包金额总和
        /// </summary>
        public decimal ReceivePacketsMoneyCount { get; set; }
        /// <summary>
        /// 发送的红包金额总和
        /// </summary>
        public decimal SendPacketsMoneyCount { get; set; }

        public int CountReceivePacket { get; set; }

        public int CountSendPacket { get; set; }

    }

    public class WeChatOrder
    {
        [BsonId]
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId WeChatOrderID { get; set; }
        /// <summary>
        /// 红包对象
        /// </summary>
        public VoicePacketsModel VoicePackets { get; set; }
        /// <summary>
        /// 订单金额
        /// </summary>
        public decimal Total { get; set; }
        /// <summary>
        /// 微信订单号
        /// </summary>
        public string WXOrderId { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonConverter(typeof(Tools.Json.DateConverterEndMinute))]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; }

    }

    public class Statement
    {
        public string StatementName { get; set; }
        [JsonConverter(typeof(Tools.Json.DateConverterEndMinute))]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; }
        public decimal RMB { get; set; }
    }
  

    public enum OrderStatus
    {
        cancel=-1,waitingPay=0,paid=1
    }
    /// <summary>
    /// 红包动作分类（领取、发出）
    /// </summary>
    public enum PacketsDoType
    {
        /// <summary>
        /// 发出
        /// </summary>
        send=0,
        /// <summary>
        /// 领取
        /// </summary>
        receive=1
    }
}
