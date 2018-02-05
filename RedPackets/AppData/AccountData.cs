using BaiduVoiceDAL;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RedPackets.Managers;
using RedPackets.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tools;
using Tools.Models;
using Tools.ResponseModels;
using We7Tools;
using WXSmallAppCommon.Models;
using WXSmallAppCommon.WXInteractions;
using WXSmallAppCommon.WXTool;

namespace RedPackets.AppData
{
    public class AccountData : BaseData<AccountModel>
    {

        /// <summary>
        /// 调取微信用户，更新或者保存本地用户
        /// </summary>
        /// <param name="wXAccount">微信用户</param>
        /// <returns></returns>
        internal AccountModel SaveOrdUpdateAccount(string uniacid, WXAccountInfo wXAccount)
        {
            Console.WriteLine("在SaveOrdUpdateAccount");
            AccountModel accountCard = null;
            if (wXAccount.OpenId != null)
            {
                var filter = Builders<AccountModel>.Filter.Eq(x => x.OpenID, wXAccount.OpenId) &
                   Builders<AccountModel>.Filter.Eq(x => x.uniacid, uniacid);
                var update = Builders<AccountModel>.Update.Set(x => x.LastChangeTime, DateTime.Now);
                accountCard = collection.FindOneAndUpdate<AccountModel>(filter, update);
                Console.WriteLine($"在SaveOrdUpdateAccount{accountCard == null}");

                if (accountCard == null)
                {
                    //string avatarUrl = DownloadAvatar(wXAccount.AvatarUrl, wXAccount.OpenId);
                    string avatarUrl = wXAccount.AvatarUrl;
                    accountCard = new AccountModel() { uniacid = uniacid, OpenID = wXAccount.OpenId, AccountName = wXAccount.NickName, Gender = wXAccount.GetGender, AccountAvatar = avatarUrl, CreateTime = DateTime.Now, LastChangeTime = DateTime.Now };
                    collection.InsertOne(accountCard);
                }
            }
            return accountCard;
        }

        internal bool CheckRelease(string uniacid)
        {
            return mongo.GetMongoCollection<CompanyModel>().Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault().IsRelease;
        }

        internal Task<string> GetFileWord(string uniacid, IFormFile file, out string filePathName, out string fileName)
        {
            long size = 0;

            filePathName = ContentDispositionHeaderValue
                                   .Parse(file.ContentDisposition)
                                   .FileName
                                   .Trim('"');
            string saveDir = $@"{ConstantProperty.BaseDir}{ConstantProperty.TempDir}";
            //string dbSaveDir = $@"{ConstantProperty.AlbumDir}{uniacid}/";
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            string exString = filePathName.Substring(filePathName.LastIndexOf("."));
            string saveName = Guid.NewGuid().ToString("N");
            filePathName = $@"{saveDir}{saveName}{exString}";

            size += file.Length;
            using (FileStream fs = System.IO.File.Create(filePathName))
            {
                file.CopyTo(fs);
                fs.Flush();
                fileName = $@"{saveName}{exString}";
            }
            var filePcmName = $@"{saveDir}{saveName}.pcm";
            Process process = new Process();
            process.StartInfo.FileName = @"cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            process.StandardInput.WriteLine("cd /home/webApp");
            process.StandardInput.WriteLine($@"silk_v3_decoder.exe {filePathName} {filePcmName} -Fs_API 8000");
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();

            return new VoiceConverter().AsrData(filePcmName);
        }

        /// <summary>
        /// 获取红包信息
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="packetsID"></param>
        /// <returns></returns>
        internal VoicePacketsModel GetPacketsInfo(string uniacid, ObjectId packetsID, ObjectId accountID)
        {
            var pmCollection = mongo.GetMongoCollection<VoicePacketsModel>();
            var pm = pmCollection.Find(x => x.uniacid.Equals(uniacid) && x.PacketsID.Equals(packetsID)).FirstOrDefault();
            pm.CheckCurrentAccountOpened(accountID);
            return pm;

        }

        /// <summary>
        /// 创建语音口令红包
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <param name="packetsModel"></param>
        /// <returns></returns>
        internal async Task<string> CreateVoicePackets(string uniacid, ObjectId accountID, VoicePacketsModel packetsModel)
        {
            CheckPeopleNum(packetsModel);
            CheckText(packetsModel.TextCmd);

            packetsModel.uniacid = uniacid;
            packetsModel.CreateTime = DateTime.Now;
            return await Task.Run(() =>
            {
                var account = GetModelByIDAndUniacID(accountID, uniacid);
                if (account == null)
                {
                    var e = new ExceptionModel()
                    {
                        MethodFullName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName,
                        Content = $"用户为空：accountID={accountID.ToString()},uniacid={uniacid}",
                        ExceptionDate = DateTime.Now
                    };
                    e.Save();
                    throw e;
                }

                var serviceMoney = GetServiceMoney(uniacid, packetsModel.Amount);
                var balance = account.Balances - (packetsModel.Amount + serviceMoney);

                packetsModel.PacketsID = ObjectId.GenerateNewId();
                packetsModel.CreatePeople = new Participant
                {
                    AccountID = account.AccountID,
                    AccountAvatar = account.AccountAvatar,
                    AccountName = account.AccountName,
                    CreateTime = DateTime.Now
                };
                if (balance < 0)
                {
                    WeChatOrder weChatOrder;
                    WXPayModel wXPayModel = GetCreatePacketsPayParams(-balance, packetsModel, uniacid, account, out weChatOrder);
                    return new BaseResponseModel3<bool, string, WXPayModel>() { StatusCode = ActionParams.code_ok, JsonData = false, JsonData1 = packetsModel.PacketsID.ToString(), JsonData2 = wXPayModel }.ToJson();
                }
                CreateVoicePackets(account, packetsModel, serviceMoney, balance);
                return new BaseResponseModel2<bool, string>() { StatusCode = ActionParams.code_ok, JsonData = true, JsonData1 = packetsModel.PacketsID.ToString() }.ToJson();
            });
        }
        /// <summary>
        /// 检测口令是否为汉字
        /// </summary>
        /// <param name="textCmd"></param>
        private void CheckText(string text)
        {
            string textCmd = (string)text.Clone();
            textCmd = textCmd.Replace("，", "");
            textCmd = textCmd.Replace(",", "");
            textCmd = textCmd.Replace("?", "");
            textCmd = textCmd.Replace("？", "");
            textCmd = textCmd.Replace(".", "");
            textCmd = textCmd.Replace("。", "");
            textCmd = textCmd.Replace(":", "");
            textCmd = textCmd.Replace("：", "");
            textCmd = textCmd.Replace("!", "");
            textCmd = textCmd.Replace("！", "");
            textCmd = textCmd.Trim();
            for (int i = 0; i < textCmd.Length; i++)
            {
                if (!Regex.IsMatch(textCmd[i].ToString(), @"[\u4e00-\u9fbb]+$"))
                {
                    var e = new ExceptionModel()
                    {
                        MethodFullName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName,
                        ExceptionDate = DateTime.Now,
                        ExceptionParam = ActionParams.packets_text_no_chinese
                    };
                    e.Save();
                    throw e;
                }
            }
        }

        /// <summary>
        /// 检测人数
        /// </summary>
        /// <param name="packetsModel"></param>
        /// <returns></returns>
        private bool CheckPeopleNum(VoicePacketsModel packetsModel)
        {
            if (packetsModel.PeopleNum == 0)
            {
                var e = new ExceptionModel()
                {
                    MethodFullName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName,
                    ExceptionDate = DateTime.Now,
                    ExceptionParam = ActionParams.packets_people_none
                };
                e.Save();
                throw e;
            }
            if (packetsModel.Amount.ConvertToMoneyCent() / packetsModel.PeopleNum < 1)
            {
                var e = new ExceptionModel()
                {
                    MethodFullName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName,
                    ExceptionDate = DateTime.Now,
                    ExceptionParam = ActionParams.packets_people_too_many
                };
                e.Save();
                throw e;
            }
            return true;
        }

        /// <summary>
        /// 执行创建语音口令红包
        /// </summary>
        /// <param name="account"></param>
        /// <param name="packetsModel"></param>
        /// <param name="serviceMoney"></param>
        /// <param name="balance"></param>
        internal void CreateVoicePackets(AccountModel account, VoicePacketsModel packetsModel, decimal serviceMoney, decimal balance)
        {
            if (account.Statements == null)
            {
                collection.UpdateOne(x => x.AccountID.Equals(account.AccountID), Builders<AccountModel>.Update.Set(x => x.Statements, new List<Statement>()));
            }

            var sp = new Statement[] {
                new Statement()
            {
                StatementName = "创建红包-手续费",
                RMB = -serviceMoney,
                CreateTime = DateTime.Now
            },
                new Statement()
            {
                StatementName = "创建红包",
                RMB = -packetsModel.Amount,
                CreateTime = DateTime.Now
            } };
            if (account.SendPackets == null)
            {
                collection.UpdateOne(x => x.AccountID.Equals(account.AccountID), Builders<AccountModel>.Update
                    .Set(x => x.SendPackets, new List<VoicePacketsModel>()));
            }

            collection.UpdateOne(x => x.AccountID.Equals(account.AccountID),
                Builders<AccountModel>.Update
                .Set(x => x.CountSendPacket, account.CountSendPacket + 1)
                .Set(x => x.SendPacketsMoneyCount, account.SendPacketsMoneyCount + packetsModel.Amount)
                .Set(x => x.Balances, balance)
                .Push(x => x.SendPackets, packetsModel)
                .PushEach(x => x.Statements, sp));
            mongo.GetMongoCollection<VoicePacketsModel>().InsertOne(packetsModel);
        }

        /// <summary>
        /// 获取收支明细
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <returns></returns>
        internal List<Statement> GetStatementList(string uniacid, ObjectId accountID)
        {
            var account = GetModelByIDAndUniacID(accountID, uniacid);
            return account.Statements.OrderByDescending(x => x.CreateTime).ToList();
        }

        /// <summary>
        /// 获取红包列表
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal List<VoicePacketsModel> GetPacketsList(string uniacid, ObjectId accountID, PacketsDoType type)
        {
            var account = GetModelByIDAndUniacID(accountID, uniacid);
            var vpmCollection = mongo.GetMongoCollection<VoicePacketsModel>();
            switch (type)
            {
                case PacketsDoType.send:
                    if (account.SendPackets == null)
                    {
                        var em = new ExceptionModel() { ExceptionParam = ActionParams.code_null };
                        em.Save();
                        throw em;
                    }
                    for (int i = 0; i < account.SendPackets.Count; i++)
                    {
                        account.SendPackets[i] = vpmCollection.Find(x => x.PacketsID.Equals(account.SendPackets[i].PacketsID)).FirstOrDefault();
                    }
                    return account.SendPackets;
                case PacketsDoType.receive:
                    if (account.ReceivePackets == null)
                    {
                        var em = new ExceptionModel() { ExceptionParam = ActionParams.code_null };
                        em.Save();
                        throw em;
                    }
                    for (int i = 0; i < account.ReceivePackets.Count; i++)
                    {
                        account.ReceivePackets[i] = vpmCollection.Find(x => x.PacketsID.Equals(account.ReceivePackets[i].PacketsID)).FirstOrDefault();
                    }
                    return account.ReceivePackets;
                default:
                    var e = new ExceptionModel() { Content = "获取红包列表type类型未知" };
                    e.Save();
                    throw e;
            }
        }

        /// <summary>
        /// 充值
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="objectId"></param>
        /// <param name="money"></param>
        /// <returns></returns>
        internal WXPayModel PushBalance(string uniacid, ObjectId accountID, decimal money)
        {
            WeChatOrder weChatOrder;
            return GetCreatePacketsPayParams(money, null, uniacid, GetModelByIDAndUniacID(accountID, uniacid), out weChatOrder);
        }

        /// <summary>
        /// 提现完成
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <param name="money"></param>
        internal void PullBalanceFinish(string uniacid, ObjectId accountID, decimal money)
        {
            var account = GetModelByIDAndUniacID(accountID, uniacid);
            var balances = account.Balances - money;
            if (balances < 0)
            {
                var em = new ExceptionModel() { ExceptionParam = ActionParams.code_insufficient_balance };
                em.Save();
                throw em;
            }
            collection.UpdateOne(x => x.AccountID.Equals(accountID), Builders<AccountModel>.Update.Set(x => x.Balances, balances));
        }

        /// <summary>
        /// 提现
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="objectId"></param>
        /// <param name="money"></param>
        internal void PullBalance(string uniacid, ObjectId accountID, decimal money)
        {
            var account = GetModelByIDAndUniacID(accountID, uniacid);
            if (account.Balances - money < 0)
            {
                var em = new ExceptionModel() { ExceptionParam = ActionParams.code_insufficient_balance };
                em.Save();
                throw em;
            }
            WeChatOrder weChatOrder;
            DoCompanyPay(money, uniacid, account, "提现", out weChatOrder);

        }

        internal List<VoicePacketsModel> GetRanklist(string uniacid)
        {
            //var pmCollection = mongo.GetMongoCollection<VoicePacketsModel>();
            //var filter = Builders<VoicePacketsModel>.Filter;
            //var filterSum = filter.Eq(x=>x.uniacid,uniacid);
            //var list = pmCollection.Aggregate().Group(new BsonDocument{
            //            { "_id", "$Participants.$.AccountID" },
            //            { "SumMoney",new BsonDocument("$sum", "$Participants.$.MoneyGet" )}
            //        });
            var list = mongo.GetMongoCollection<VoicePacketsModel>().Find(x => x.uniacid.Equals(uniacid)).ToList();
            var sortList = list.OrderByDescending(x => x.Amount).ToList();
            for (int i = 0; i < sortList.Count; i++)
            {
                sortList[i].Top = i + 1;
            }
            return sortList.Count > 50 ? sortList.GetRange(0, 50) : sortList;
            //return collection.Find(x => x.uniacid.Equals(uniacid)).SortBy(x => x.ReceivePacketsMoneyCount).ToList();

        }

        internal AccountModel GetAccountInfo(string uniacid, ObjectId accountID)
        {
            var account = GetModelByIDAndUniacID(accountID, uniacid);
            return account;
        }

        /// <summary>
        /// 开启红包
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="accountID"></param>
        /// <param name="packetsID"></param>
        /// <returns></returns>
        internal async Task<ActionParams> OpenRedPacketsAsync(string uniacid, ObjectId accountID, ObjectId packetsID, IFormFile file)
        {
            try
            {
                string fileName, filePathName;
                string word = await GetFileWord(uniacid, file, out filePathName, out fileName);
                var filter = Builders<VoicePacketsModel>.Filter;
                var filterSum = filter.Eq(x => x.uniacid, uniacid) & filter.Eq(x => x.PacketsID, packetsID);
                var pmCollection = mongo.GetMongoCollection<VoicePacketsModel>();
                var pm = pmCollection.Find(filterSum).FirstOrDefault();
                CompareCmd(word, pm.TextCmd);
                NoOpenPackets(accountID, pm);
                decimal money = CalcMoney(pm);
                var account = GetModelByIDAndUniacID(accountID, uniacid);
                var participant = new Participant()
                {
                    AccountAvatar = account.AccountAvatar,
                    AccountID = account.AccountID,
                    AccountName = account.AccountName,
                    CreateTime = DateTime.Now,
                    MoneyGet = money,
                    VoiceFileName = fileName
                };
                if (pm.Participants == null)
                {
                    pmCollection.UpdateOne(filterSum, Builders<VoicePacketsModel>.Update.Set(x => x.Participants, new List<Participant>()));
                }
                var update = Builders<VoicePacketsModel>.Update.Push(x => x.Participants, participant);
                pmCollection.UpdateOne(filterSum, update);
                FileManager.Exerciser(uniacid, filePathName, null).SaveFile();
                PushMoneyToBalance(uniacid, account, pm, money);
                return ActionParams.code_ok;
            }
            catch (ExceptionModel em)
            {
                return em.ExceptionParam;
                throw em;
            }

        }

        /// <summary>
        /// 将红包放入余额
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="account"></param>
        /// <param name="money"></param>
        private void PushMoneyToBalance(string uniacid, AccountModel account, VoicePacketsModel vpm, decimal money)
        {
            var createPeople = GetModelByID(vpm.CreatePeople.AccountID);
            var filter = Builders<AccountModel>.Filter;
            var filterSum = filter.Eq(x => x.uniacid, uniacid) & filter.Eq(x => x.AccountID, account.AccountID);
            if (account.ReceivePackets == null)
            {
                collection.UpdateOne(filterSum, Builders<AccountModel>.Update.Set(x => x.ReceivePackets, new List<VoicePacketsModel>()));
            }
            if (account.Statements == null)
            {
                collection.UpdateOne(x => x.AccountID.Equals(account.AccountID), Builders<AccountModel>.Update.Set(x => x.Statements, new List<Statement>()));
            }
            var packetsStatement = new Statement()
            {
                StatementName = $@"领取{createPeople.AccountName}的红包",
                RMB = +money,
                CreateTime = DateTime.Now
            };
            var update = Builders<AccountModel>.Update
                .Set(x => x.Balances, account.Balances + money)
                .Set(x => x.ReceivePacketsMoneyCount, account.ReceivePacketsMoneyCount + money)
                .Set(x => x.CountReceivePacket, account.CountReceivePacket + 1)
                .Push(x => x.ReceivePackets, vpm)
                .Push(x => x.Statements, packetsStatement);
            collection.UpdateOne(filterSum, update);


        }

        /// <summary>
        /// 没有领取红包
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        private bool NoOpenPackets(ObjectId accountID, VoicePacketsModel pm)
        {
            bool flag = true;
            if (pm.Participants == null)
            {
                return flag;
            }
            if (pm.Participants.Count >= pm.PeopleNum)
            {
                flag = false;
                throw (new ExceptionModel()
                {
                    ExceptionParam = ActionParams.packets_opened
                });
            }
            if (pm.Participants.Exists(x => x.AccountID.Equals(accountID)))
            {
                flag = false;
                throw (new ExceptionModel()
                {
                    ExceptionParam = ActionParams.packets_people_opened
                });
            }
            return flag;
        }

        /// <summary>
        /// 计算红包金额
        /// </summary>
        /// <param name="pm"></param>
        /// <returns></returns>
        private decimal CalcMoney(VoicePacketsModel pm)
        {
            decimal sumPeopleMoney = 0;
            if (pm.Participants != null)
            {
                pm.Participants.ForEach(x => sumPeopleMoney += x.MoneyGet);
            }

            decimal amount = pm.Amount - sumPeopleMoney;
            if (pm.Participants != null && pm.Participants.Count + 1 == pm.PeopleNum)
            {
                return amount;
            }
            if (pm.PeopleNum == 1)
            {
                return pm.Amount;
            }
            int cent = amount.ConvertToMoneyCent();
            int randomCent = new Random().Next(1, cent - (pm.PeopleNum - (pm.Participants == null ? 0 : pm.Participants.Count)));
            return (decimal)randomCent / (decimal)100;
        }

        /// <summary>
        /// 对比口令
        /// </summary>
        /// <param name="word"></param>
        /// <param name="textCmd"></param>
        /// <returns></returns>
        private void CompareCmd(string word, string textCmd)
        {
            //new ExceptionModel() { Content = $@"{word}<compare>{textCmd}" }.Save();
            word = word.Replace("，", "");
            word = word.Replace(",", "");
            word = word.Replace("?", "");
            word = word.Replace("？", "");
            word = word.Replace(".", "");
            word = word.Replace("。", "");
            word = word.Replace(":", "");
            word = word.Replace("：", "");
            word = word.Replace("!", "");
            word = word.Replace("！", "");
            word = word.Trim();

            textCmd = textCmd.Replace("，", "");
            textCmd = textCmd.Replace(",", "");
            textCmd = textCmd.Replace("?", "");
            textCmd = textCmd.Replace("？", "");
            textCmd = textCmd.Replace(".", "");
            textCmd = textCmd.Replace("。", "");
            textCmd = textCmd.Replace(":", "");
            textCmd = textCmd.Replace("：", "");
            textCmd = textCmd.Replace("!", "");
            textCmd = textCmd.Replace("！", "");
            textCmd = textCmd.Trim();
            bool eq = textCmd.Equals(word);
            if (!eq)
            {
                var em = new ExceptionModel() { Content = $@"{word}<compare:{eq}>{textCmd}", ExceptionParam = ActionParams.code_error_verify };
                em.Save();
                throw em;
            }

        }

        /// <summary>
        /// 创建微信支付参数
        /// </summary>
        /// <param name="money"></param>
        /// <param name="vpm"></param>
        /// <param name="uniacid"></param>
        /// <param name="account"></param>
        /// <param name="weChatOrder"></param>
        /// <returns></returns>
        private WXPayModel GetCreatePacketsPayParams(decimal money, VoicePacketsModel vpm, string uniacid, AccountModel account, out WeChatOrder weChatOrder)
        {
            weChatOrder = new WeChatOrder()
            {
                CreateTime = DateTime.Now,
                Total = money,
                VoicePackets = vpm,
                WeChatOrderID = ObjectId.GenerateNewId()
            };
            if (account.WeChatOrders == null)
            {
                collection.UpdateOne(x => x.AccountID.Equals(account.AccountID),
                    Builders<AccountModel>.Update.Set(x => x.WeChatOrders, new List<WeChatOrder>()));
            }
            collection.UpdateOne(x => x.AccountID.Equals(account.AccountID),
                    Builders<AccountModel>.Update.Push(x => x.WeChatOrders, weChatOrder));
            ///微擎相关
            JsApiPay jsApiPay = new JsApiPay();
            jsApiPay.openid = account.OpenID;
            jsApiPay.total_fee = money.ConvertToMoneyCent();
            var body = "test";
            var attach = account.AccountID + "," + weChatOrder.WeChatOrderID.ToString();
            var goods_tag = "创建红包";
            jsApiPay.CreateWeChatOrder(uniacid, body, attach, goods_tag);
            var param = jsApiPay.GetJsApiParameters(We7Tools.Models.We7ProcessMiniConfig.GetAllConfig(uniacid).KEY);
            var wxpm = JsonConvert.DeserializeObject<WXPayModel>(param);
            return wxpm;
        }

        /// <summary>
        /// 商户进行支付
        /// </summary>
        /// <param name="money"></param>
        /// <param name="vpm"></param>
        /// <param name="uniacid"></param>
        /// <param name="account"></param>
        /// <param name="weChatOrder"></param>
        /// <returns></returns>
        private WxPayData DoCompanyPay(decimal money, string uniacid, AccountModel account, string desc, out WeChatOrder weChatOrder)
        {
            weChatOrder = new WeChatOrder()
            {
                CreateTime = DateTime.Now,
                Total = money,
                WeChatOrderID = ObjectId.GenerateNewId()
            };

            if (account.WeChatOrders == null)
            {
                collection.UpdateOne(x => x.AccountID.Equals(account.AccountID),
                    Builders<AccountModel>.Update.Set(x => x.WeChatOrders, new List<WeChatOrder>()));
            }

            collection.UpdateOne(x => x.AccountID.Equals(account.AccountID),
                    Builders<AccountModel>.Update.Push(x => x.WeChatOrders, weChatOrder));

            return We7Tools.We7Tools.DoTransfer(uniacid, money, desc, account.OpenID, weChatOrder.WeChatOrderID.ToString());
        }


        /// <summary>
        /// 获取手续费
        /// </summary>
        /// <param name="uniacid"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal decimal GetServiceMoney(string uniacid, decimal amount)
        {
            var company = mongo.GetMongoCollection<CompanyModel>().Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
            if (company == null)
            {
                var e = new ExceptionModel()
                {
                    MethodFullName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName,
                    Content = $"商户为空：uniacid={uniacid}",
                    ExceptionDate = DateTime.Now
                };
                e.Save();
                throw e;
            }
            return Math.Round(amount * company.ServiceRate, 2, MidpointRounding.AwayFromZero);
        }

    }
}
