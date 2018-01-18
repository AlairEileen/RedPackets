using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using RedPackets.Managers;
using RedPackets.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tools;
using Tools.Models;

namespace RedPackets.AppData
{
    public class MerchantData : BaseData<MerchantModel>
    {
        internal void SetQiNiu(string uniacid, QiNiuModel qiNiuModel)
        {
            var companyCollection = mongo.GetMongoCollection<CompanyModel>();
            var company = companyCollection.Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
            if (company == null)
            {
                companyCollection.InsertOne(new CompanyModel()
                {
                    uniacid = uniacid,
                    QiNiuModel = qiNiuModel
                });
            }
            else
            {
                companyCollection.UpdateOne(x => x.uniacid.Equals(uniacid), Builders<CompanyModel>.Update.Set(x => x.QiNiuModel, qiNiuModel));
            }
        }
        internal CompanyModel GetCompanyModel(string uniacid)
        {
            var companyModel = mongo.GetMongoCollection<CompanyModel>().Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
            return companyModel;
        }

        internal void SetServiceRate(string uniacid, decimal serviceRate)
        {
            var companyCollection = mongo.GetMongoCollection<CompanyModel>();
            var company = companyCollection.Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
            if (company == null)
            {
                companyCollection.InsertOne(new CompanyModel()
                {
                    uniacid = uniacid,
                    ServiceRate = serviceRate
                });
            }
            else
            {
                companyCollection.UpdateOne(x => x.uniacid.Equals(uniacid), Builders<CompanyModel>.Update.Set(x => x.ServiceRate, serviceRate));
            }
        }

        internal void PushCert(string uniacid, IFormFile file)
        {
            long size = 0;
            var filename = ContentDispositionHeaderValue
                                  .Parse(file.ContentDisposition)
                                  .FileName
                                  .Trim('"');
            string dbSaveDir = $@"{ConstantProperty.CertsDir}{uniacid}/";
            string saveDir = $@"{ConstantProperty.BaseDir}{dbSaveDir}";
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            string[] files = Directory.GetFiles(saveDir);
            foreach (var item in files)
            {
                File.Delete(item);
            }

            string exString = filename.Substring(filename.LastIndexOf("."));
            string saveName = Guid.NewGuid().ToString("N");
            filename = $@"{saveDir}{saveName}{exString}";

            size += file.Length;
            using (FileStream fs = System.IO.File.Create(filename))
            {
                file.CopyTo(fs);
                fs.Flush();
                string[] fileUrls = new string[] { $@"{dbSaveDir}{saveName}{exString}" };
                //FileManager.Exerciser(uniacid, filename, null).SaveFile();

                var companyCollection = mongo.GetMongoCollection<CompanyModel>();
                var company = companyCollection.Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
                if (company == null)
                {
                    companyCollection.InsertOne(new CompanyModel()
                    {
                        uniacid = uniacid,
                        CertFileName = $"{saveName}{exString}"
                    });
                }
                else
                {
                    companyCollection.UpdateOne(x => x.uniacid.Equals(uniacid), Builders<CompanyModel>.Update.Set(x => x.CertFileName, $"{saveName}{exString}"));
                }
            }
        }
    }
}
