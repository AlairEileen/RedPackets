using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using RedPackets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tools;
using Tools.DB;
using Tools.ResponseModels;
using WXSmallAppCommon.WXInteractions;
using WXSmallAppCommon.WXTool;

namespace RedPackets.Controllers
{
    public class TestController:Controller
    {
        public string TestTransfer(int amount)
        {
            WxPayData wpd = Transfer.Run(amount, "你好雅心!", "okn8I0bp6xDylvYC9jlVwV9yGnFQ", ObjectId.GenerateNewId().ToString());
            return wpd.ToPrintStr();
        }

        public string RefundAll(string accountID)
        {
            var account = new MongoDBTool().GetMongoCollection<AccountModel>().Find(x => x.AccountID.Equals(new ObjectId(accountID))).FirstOrDefault();
            if (account==null)
            {
                return "账户不存在";
            }
            if (account.WeChatOrders==null)
            {
                return "账户订单不存在";
            }
            List<string> wxOrderList = new List<string>();
            account.WeChatOrders.ForEach(x=> {
                if (!string.IsNullOrEmpty(x.WXOrderId))
                {
                    wxOrderList.Add(x.WXOrderId);
                    Refund.Run(x.WXOrderId, null, x.Total.ConvertToMoneyCent(), x.Total.ConvertToMoneyCent());
                    Thread.Sleep(200);
                }
            });
            
            return new BaseResponseModel<List<string>>() { JsonData = wxOrderList }.ToJson();
        }
    }
}
