using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigData
{
    public class We7Config
    {
        /// <summary>
        /// 小程序文件夹目录
        /// </summary>
        public const string ProcessMiniFolderPath = MainConfig.BaseDir + @"processMini/";
        public const string We7DataSessionName = "redPacketsWe7Data";
        public const string SiteRoot = "https://xcxh.360yingketong.com/"+(MainConfig.IsDev?"RedPackets/": "red_packets/");
        public const string PMVersion = "1.0";

        /// <summary>
        /// 微擎相关
        /// </summary>
        public const string We7ProjName ="red_packets";
        public const string We7Domain = "https://"+(MainConfig.IsDev?"dev":"xcx")+".360yingketong.com/addons/";
        public const string We7DataGetUrl = We7Domain + We7ProjName + "/db_select.php?key=5a5edafbed06c11a1829a4f6&uniacid=";

    }
}
