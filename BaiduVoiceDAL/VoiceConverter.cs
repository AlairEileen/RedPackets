using Baidu.Aip.Speech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaiduVoiceDAL
{
    public class VoiceConverter
    {

        private string serverURL = "http://vop.baidu.com/server_api";
        private static string token = "";
        private static string testFileName = "test.pcm";
        //put your own params here
        private static string apiKey = "s8M5pRA7yL9w586RmeoRxo53";
        private static string secretKey = "2ab7cd180d1377cbb8bf3bd4a7e62e7f";
        private static string cuid = "666";

        private readonly Asr _asrClient;
        private readonly Tts _ttsClient;
        public VoiceConverter()
        {
            _asrClient = new Asr(apiKey, secretKey);
            _ttsClient = new Tts(apiKey, secretKey);
        }
        // 识别本地文件
        public async Task<string> AsrData(string fileUrl)
        {
            return
              await Task.Run(() =>
               {

                   var data = File.ReadAllBytes(fileUrl);
                   var result = _asrClient.Recognize(data, "pcm", 8000);
                   return JsonConvert.DeserializeObject<string[]>(result["result"].ToString())[0];
               });
        }

        public async Task<string> GetToken()
        {
            return await Task.Run(() =>
            {
                string accessToken = "";
                String getTokenURL = "https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials" +
           "&client_id=" + apiKey + "&client_secret=" + secretKey;
                WebClient wc = new WebClient();
                string responseString = wc.DownloadString(getTokenURL);
                var jObj = (JObject)JsonConvert.DeserializeObject(responseString);
                accessToken = jObj.GetValue("access_token").ToString();
                return accessToken;
            });
        }

        public async Task<string> Post(string audioFilePath)
        {
            string token = await GetToken();
            serverURL += "?lan=zh&cuid=kwwwvagaa&token=" + token;
            byte[] voice = File.ReadAllBytes(audioFilePath);


            HttpWebRequest request = null;



            Uri uri = new Uri(serverURL);
            request = (HttpWebRequest)WebRequest.Create(uri);
            request.Timeout = 10000;
            request.Method = "POST";
            request.ContentType = "audio/pcm; rate=8000";
            request.ContentLength = voice.Length;
            try
            {
                using (Stream writeStream = request.GetRequestStream())
                {
                    writeStream.Write(voice, 0, voice.Length);
                    writeStream.Close();
                    writeStream.Dispose();
                }
            }
            catch
            {
                return null;
            }
            string result = string.Empty;
            string result_final = string.Empty;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string line = string.Empty;
                        StringBuilder sb = new StringBuilder();
                        while (!readStream.EndOfStream)
                        {
                            line = readStream.ReadLine();
                            sb.Append(line);
                            sb.Append("\r");
                        }
                        readStream.Close();
                        readStream.Dispose();
                        result = sb.ToString();
                        string[] indexs = result.Split(',');
                        foreach (string index in indexs)
                        {
                            string[] _indexs = index.Split('"');
                            if (_indexs[2] == ":[")
                                result_final = _indexs[3];
                        }
                    }
                    responseStream.Close();
                    responseStream.Dispose();
                }
                response.Close();
            }
            return result_final;
        }
    }
}
