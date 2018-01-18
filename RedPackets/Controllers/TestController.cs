using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
