using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using RedPackets.AppData;
using RedPackets.Managers;
using RedPackets.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tools;
using Tools.Json;
using Tools.Models;
using Tools.Response;
using Tools.ResponseModels;
using We7Tools;
using We7Tools.Extend;
using WXSmallAppCommon.Models;

namespace RedPackets.Controllers
{
    public class AccountController : BaseController<AccountData, AccountModel>
    {

        /// <summary>
        /// 请求登录
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="code"></param>
        /// <param name="iv"></param>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        [HttpGet]
        public string GetAccountID(string uniacid, string code, string iv, string encryptedData)
        {
            try
            {
                BaseResponseModel<AccountModel> responseModel = new BaseResponseModel<AccountModel>();

                //WXSmallAppCommon.Models.WXAccountInfo wXAccount = WXSmallAppCommon.WXInteractions.WXLoginAction.ProcessRequest(code, iv, encryptedData);
                ///微擎方式
                WXSmallAppCommon.Models.WXAccountInfo wXAccount = We7Tools.We7Tools.GetWeChatUserInfo(uniacid, code, iv, encryptedData);
                var accountCard = thisData.SaveOrdUpdateAccount(uniacid, wXAccount);
                ActionParams stautsCode = ActionParams.code_error;
                if (accountCard != null)
                {
                    responseModel.JsonData = accountCard;
                    stautsCode = ActionParams.code_ok;
                }
                responseModel.StatusCode = stautsCode;
                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
                string[] param = new string[] { "StatusCode", "JsonData", "AccountID" };
                jsonSerializerSettings.ContractResolver = new LimitPropsContractResolver(param);
                string jsonString = JsonConvert.SerializeObject(responseModel, jsonSerializerSettings);
                return jsonString;
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

        /// <summary>
        /// 创建红包
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">账户ID</param>
        /// <param name="packetsType">红包类型（0：语音口令红包）</param>
        /// <returns></returns>
        public async Task<string> CreateRedPacketsCmd(string uniacid, string accountID, PacketsType packetsType)
        {
            try
            {
                string json = new StreamReader(Request.Body).ReadToEnd();
                switch (packetsType)
                {
                    case PacketsType.Voice:
                        var packetsModel = JsonConvert.DeserializeObject<VoicePacketsModel>(json);
                        return await thisData.CreateVoicePackets(uniacid, new ObjectId(accountID), packetsModel);
                    default:
                        return JsonResponseModel.ErrorJson;
                }
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }

        /// <summary>
        /// 获取红包信息
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="packetsID">订单ID或者红包ID</param>
        /// <returns></returns>
        public string GetPacketsInfo(string uniacid, string packetsID)
        {
            try
            {
                VoicePacketsModel vpm = thisData.GetPacketsInfo(uniacid, new ObjectId(packetsID));
                return new BaseResponseModel<VoicePacketsModel>() { StatusCode = ActionParams.code_ok, JsonData = vpm }.ToJson();
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }

        /// <summary>
        /// 开启红包
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">领取者账户</param>
        /// <param name="packetsID">红包ID</param>
        /// <returns></returns>
        public async Task<string> OpenRedPackets(string uniacid, string accountID, string packetsID)
        {
            try
            {
                var files = Request.Form.Files;
                VoicePacketsModel vpm = await thisData.OpenRedPacketsAsync(uniacid, new ObjectId(accountID), new ObjectId(packetsID), files[0]);
                return new BaseResponseModel<VoicePacketsModel>() { StatusCode = ActionParams.code_ok, JsonData = vpm }.ToJson();
            }
            catch (ExceptionModel em)
            {
                em.Save();
                return JsonResponseModel.OtherJson(em.ExceptionParam);
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }

        /// <summary>
        /// 获取账户信息
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">账户ID</param>
        /// <returns></returns>
        public string GetAccountInfo(string uniacid, string accountID)
        {
            try
            {
                AccountModel account = thisData.GetAccountInfo(uniacid, new ObjectId(accountID));
                var responseModel = new BaseResponseModel<AccountModel>() { JsonData = account, StatusCode = ActionParams.code_ok };
                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
                string[] param = new string[] { "StatusCode", "JsonData", "AccountID", "Balances" };
                jsonSerializerSettings.ContractResolver = new LimitPropsContractResolver(param);
                string jsonString = JsonConvert.SerializeObject(responseModel, jsonSerializerSettings);
                return jsonString;
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

        /// <summary>
        /// 获取排行榜
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <returns></returns>
        public string GetRanklist(string uniacid)
        {
            try
            {
                List<VoicePacketsModel> list = thisData.GetRanklist(uniacid);
                return new BaseResponseModel<List<VoicePacketsModel>>() { StatusCode = ActionParams.code_ok, JsonData = list }.ToJson();
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }

        /// <summary>
        /// 获取红包列表
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">账户ID</param>
        /// <param name="type">红包动作分类（领取、发出）</param>
        /// <returns></returns>
        public string GetPacketsList(string uniacid, string accountID, PacketsDoType type)
        {
            try
            {
                List<VoicePacketsModel> list = thisData.GetPacketsList(uniacid, new ObjectId(accountID), type);
                var jm = new BaseResponseModel<List<VoicePacketsModel>>() { StatusCode = ActionParams.code_ok, JsonData = list };
                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
                string[] param = new string[] { "StatusCode", "JsonData", "TextCmd", "PacketsID", "Amount", "CreateTime", "OpenStatus" };
                jsonSerializerSettings.ContractResolver = new LimitPropsContractResolver(param);
                string jsonString = JsonConvert.SerializeObject(jm, jsonSerializerSettings);

                return jsonString;
            }
            catch (Exception)
            {
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

        /// <summary>
        /// 获取收支明细
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">账户ID</param>
        /// <returns></returns>
        public string GetStatementList(string uniacid, string accountID)
        {
            try
            {
                List<Statement> list = thisData.GetStatementList(uniacid, new ObjectId(accountID));
                return new BaseResponseModel<List<Statement>>() { StatusCode = ActionParams.code_ok, JsonData = list }.ToJson();
            }
            catch (Exception)
            {
                return JsonResponseModel.ErrorJson;
                throw;
            }

        }
        
        /// <summary>
        /// 获取服务费
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="amount">金额</param>
        /// <returns></returns>
        public string GetServiceMoney(string uniacid,decimal amount)
        {
            try
            {
                var money = thisData.GetServiceMoney(uniacid, amount);
                return new BaseResponseModel<decimal>() { StatusCode = ActionParams.code_ok, JsonData = money }.ToJson();
            }
            catch (Exception)
            {
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }
       
        /// <summary>
        /// 获取音频文件
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public async Task<IActionResult> GetVoiceFileAsync(string uniacid,string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            uniacid = string.IsNullOrEmpty(uniacid) ? HttpContext.Session.GetUniacID() : uniacid;
            string fileUrl = await FileManager.Exerciser(uniacid, null, fileName).GetFile();
            var stream = System.IO.File.OpenRead(fileUrl);
            return File(stream, "application/vnd.android.package-archive", Path.GetFileName(fileUrl));
        }

        /// <summary>
        /// 充值
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <param name="money"></param>
        /// <returns></returns>
        public string PushBalance(string uniacid,string accountID,decimal money)
        {
            try
            {
                WXPayModel wpm = thisData.PushBalance(uniacid, new ObjectId(accountID), money);
                return new BaseResponseModel<WXPayModel>() { StatusCode = ActionParams.code_ok, JsonData = wpm }.ToJson();
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }
       
        /// <summary>
        /// 提现
        /// </summary>
        /// <param name="uniacid">商户识别ID</param>
        /// <param name="accountID">用户ID</param>
        /// <param name="money">提现金额</param>
        /// <returns></returns>
        public string PullBalance(string uniacid,string accountID,decimal money)
        {
            try
            {
                thisData.PullBalance(uniacid, new ObjectId(accountID), money);
                thisData.PullBalanceFinish(uniacid, new ObjectId(accountID), money);
                return JsonResponseModel.SuccessJson;
            }
            catch (ExceptionModel em)
            {
                return JsonResponseModel.OtherJson(em.ExceptionParam);
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
            }
        }

    }
}
