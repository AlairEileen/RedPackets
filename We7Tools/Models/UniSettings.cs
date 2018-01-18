using ConfigData;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Tools.DB;
using We7Tools.MysqlTool;
using WXSmallAppCommon.WXTool;

namespace We7Tools.Models
{
    internal class UniSettings
    {
        Serializer seria = new Serializer();
        internal UniSettings(string uniacid)
        {
            //连接数据库
            using (MySqlConnection msconnection = MysqlDBTool.GetConnection())
            {
                msconnection.Open();
                //查找数据库里面的表
                MySqlCommand mscommand = new MySqlCommand("select * from ims_uni_settings where uniacid=" + uniacid, msconnection);
                using (MySqlDataReader reader = mscommand.ExecuteReader())
                {
                    //读取数据
                    while (reader.Read())
                    {
                        payment = reader.GetString("payment");
                    }
                }
            }
        }
        internal void WriteConfig(ProcessMiniConfig processMiniConfig)
        {
            var obj = seria.Deserialize(payment);
            if (!(obj is Hashtable))
            {
                return;
            }
            Hashtable hasObj = (Hashtable)obj;
            var wechat = (Hashtable)hasObj["wechat"];
            if (wechat != null)
            {
                processMiniConfig.MCHID = wechat["mchid"] == null ? null : wechat["mchid"].ToString();
                processMiniConfig.KEY = wechat["signkey"] == null ? null : wechat["signkey"].ToString();
            }

            var wechat_refund = (Hashtable)hasObj["wechat_refund"];
            if (wechat_refund != null)
            {
                processMiniConfig.CertKey = wechat_refund["key"] == null ? null : wechat_refund["key"].ToString();
                processMiniConfig.cert = wechat_refund["cert"] == null ? null : wechat_refund["cert"].ToString();
            }
        }
        private string payment;
        internal string PayMentJson { get { return JsonConvert.SerializeObject(seria.Deserialize(payment)); } }

    }
    public class ProcessMiniConfig
    {
        public string MCHID { get; set; }
        public string KEY { get; set; }
        private string sSLCERT_PASSWORD;
        public string CertKey { get; set; }
        public string cert { get; set; }
        public string APPID { get; set; }
        public string APPSECRET { get; set; }
        public string SSLCERT_PASSWORD { get { return sSLCERT_PASSWORD == null ? MCHID : sSLCERT_PASSWORD; } set { sSLCERT_PASSWORD = value; } }

        public string SSLCERT_PATH { get; internal set; }
    }

    internal class AccountWXApp
    {
        private string key;
        private string secret;
        internal AccountWXApp(string uniacid)
        {
            //连接数据库
            using (MySqlConnection msconnection = MysqlDBTool.GetConnection())
            {
                msconnection.Open();
                //查找数据库里面的表
                MySqlCommand mscommand = new MySqlCommand("select * from ims_account_wxapp where uniacid=" + uniacid, msconnection);
                using (MySqlDataReader reader = mscommand.ExecuteReader())
                {
                    //读取数据
                    while (reader.Read())
                    {
                        key = reader.GetString("key");
                        secret = reader.GetString("secret");
                    }
                }
            }
        }
        internal void WriteConfig(ProcessMiniConfig processMiniConfig)
        {
            processMiniConfig.APPID = key;
            processMiniConfig.APPSECRET = secret;
        }

    }

    public class We7ProcessMiniConfig
    {
        public static void InitialWxPayConfig(string uniacid)
        {
            var config=GetAllConfig(uniacid);
            //WxPayConfig.APPID = config.APPID;
            //WxPayConfig.APPSECRET = config.APPSECRET;
            //WxPayConfig.KEY = config.KEY;
            //WxPayConfig.MCHID = config.MCHID;
            //WxPayConfig.SSLCERT_PASSWORD = config.SSLCERT_PASSWORD;
        }

      
        public static ProcessMiniConfig GetAllConfig(string uniacid)
        {
            ProcessMiniConfig pmc = new ProcessMiniConfig();
            new UniSettings(uniacid).WriteConfig(pmc);
            new AccountWXApp(uniacid).WriteConfig(pmc);
            WriteCertFileConfig(uniacid,pmc);
            return pmc;
        }

        private static void WriteCertFileConfig(string uniacid,ProcessMiniConfig pmc)
        {
            BsonDocument document = new MongoDBTool().GetMongoCollection<BsonDocument>("CompanyModel").Find(Builders<BsonDocument>.Filter.Eq("uniacid", uniacid)).FirstOrDefault();
            var cfn = document.GetValue("CertFileName").ToString();
            pmc.SSLCERT_PATH = $"{MainConfig.BaseDir}{MainConfig.CertsDir}/{uniacid}/{cfn}";
        }

        public static ProcessMiniConfig GetAppConfig(string uniacid)
        {
            ProcessMiniConfig pmc = new ProcessMiniConfig();
            new AccountWXApp(uniacid).WriteConfig(pmc);
            return pmc;
        }
        public static ProcessMiniConfig GetMCConfig(string uniacid)
        {
            ProcessMiniConfig pmc = new ProcessMiniConfig();
            new UniSettings(uniacid).WriteConfig(pmc);
            return pmc;
        }
    }


}
