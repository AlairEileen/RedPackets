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
    public class ManageModel
    {
    }
    public class CompanyModel
    {
        [BsonId]
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId CompanyID { get; set; }
        public string uniacid { get; set; }
        public ProcessMiniInfo ProcessMiniInfo { get; set; }
        public QiNiuModel QiNiuModel { get; set; }
        /// <summary>
        /// 服务费率
        /// </summary>
        public decimal ServiceRate { get; set; }
        public string CertFileName { get; set; }
        /// <summary>
        /// 是否为正式版
        /// </summary>
        public bool IsRelease { get; set; }
    }
    public class QiNiuModel
    {
        public QiNiuDAL.Exerciser exerciser = new QiNiuDAL.Exerciser();
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Bucket { get; set; }
        public string DoMain { get; set; }
        public void UploadFile(string filePath)
        {
            exerciser.UploadFile(filePath, AccessKey, SecretKey, Bucket);
        }
        public async Task<string> GetFileUrl(string fileName)
        {
            return await exerciser.CreateDownloadUrl(DoMain, fileName);
        }
        public void DeleteFile(string fileName)
        {
            exerciser.DeleteFile(fileName, AccessKey, SecretKey, Bucket);
        }
    }
    public class ProcessMiniInfo
    {
        public string Detail { get; set; }
        public string Name { get; set; }
        public FileModel<string[]> Logo { get; set; }
    }
}
