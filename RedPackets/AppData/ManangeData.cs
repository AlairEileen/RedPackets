using MongoDB.Driver;
using RedPackets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tools.Models;

namespace RedPackets.AppData
{
    public class ManangeData : BaseData<ManageModel>
    {
        internal void SetProcessMiniInfo(string uniacid, ProcessMiniInfo processMiniInfo)
        {
            var fileModel = mongo.GetMongoCollection<FileModel<string[]>>("FileModel").Find(x => x.FileID.Equals(processMiniInfo.Logo.FileID)).FirstOrDefault();
            processMiniInfo.Logo = fileModel ?? throw new Exception();
            var companyCollection = mongo.GetMongoCollection<CompanyModel>();
            var companyFilter = Builders<CompanyModel>.Filter.Eq(x => x.uniacid, uniacid);
            var company = companyCollection.Find(companyFilter).FirstOrDefault();
            if (company == null)
            {
                companyCollection.InsertOne(new CompanyModel() { uniacid = uniacid });
            }
            companyCollection.UpdateOne(companyFilter, Builders<CompanyModel>.Update.Set(x => x.ProcessMiniInfo, processMiniInfo));
        }
        internal void SetQiNiu(string uniacid, QiNiuModel qiNiuModel)
        {
            var companyCollection = mongo.GetMongoCollection<CompanyModel>();
            companyCollection.UpdateOne(x => x.uniacid.Equals(uniacid), Builders<CompanyModel>.Update.Set(x => x.QiNiuModel, qiNiuModel));
        }
        internal CompanyModel GetCompanyModel(string uniacid)
        {
            var companyModel = mongo.GetMongoCollection<CompanyModel>().Find(x => x.uniacid.Equals(uniacid)).FirstOrDefault();
            return companyModel;
        }

    }
}
