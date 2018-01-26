using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RedPackets.AppData;
using RedPackets.Models;
using Tools.Models;
using Tools.Response;
using We7Tools;
using We7Tools.Extend;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RedPackets.Controllers
{
    public class MerchantController : BaseController<MerchantData, MerchantModel>
    {

        public MerchantController() : base(true) { }

        // GET: /<controller>/
        public IActionResult Index()
        {
            List<TransactionHistoryViewModel> thList = GetTHVM();
            return View(thList);
        }

        private List<TransactionHistoryViewModel> GetTHVM()
        {
          return  thisData.GetTHList(HttpContext.Session.GetUniacID());
        }

        public IActionResult Settings()
        {
            var companyModel = thisData.GetCompanyModel(HttpContext.Session.GetUniacID());
            if (companyModel == null)
            {
                companyModel = new CompanyModel() { ProcessMiniInfo = new ProcessMiniInfo(), QiNiuModel = new QiNiuModel() };
            }
            return View(new ManageViewModel() { UploadedCert = !string.IsNullOrEmpty(companyModel.CertFileName), ProcessMiniInfo = companyModel.ProcessMiniInfo, ServiceRate = companyModel.ServiceRate, QiNiuModel = companyModel.QiNiuModel == null ? new QiNiuModel() : companyModel.QiNiuModel });
        }

        public IActionResult ProcessMiniZipDownload()
        {
            if (!HttpContext.Session.HasWe7Data())
            {
                return RedirectToAction("Index", "Error", new { errorType = ErrorType.ErrorNoUserOrTimeOut });
            }
            try
            {
                string fileUrl;
                ProcessMiniTool.GetProcessMiniZipPath(HttpContext.Session, out fileUrl);
                byte[] fileByteArray = System.IO.File.ReadAllBytes(fileUrl);
                var fileName = Path.GetFileName(fileUrl);
                System.IO.File.Delete(fileUrl);
                return File(fileByteArray, "application/vnd.android.package-archive", fileName);
            }
            catch (Exception e)
            {
                e.Save();
                return RedirectToAction("Index", "Error");
                //throw;
            }
        }
        public string SetQiNiu()
        {
            try
            {
                string json = new StreamReader(Request.Body).ReadToEnd();
                QiNiuModel qiNiuModel = JsonConvert.DeserializeObject<QiNiuModel>(json);
                thisData.SetQiNiu(HttpContext.Session.GetUniacID(), qiNiuModel);
                return JsonResponseModel.SuccessJson;
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

        /// <summary>
        /// 设置服务费率
        /// </summary>
        /// <param name="serviceRate"></param>
        /// <returns></returns>
        public string SetServiceRate(decimal serviceRate)
        {
            try
            {
                thisData.SetServiceRate(HttpContext.Session.GetUniacID(), serviceRate);
                return JsonResponseModel.SuccessJson;
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

        public string PushCert()
        {
            try
            {
                var files = Request.Form.Files;
                thisData.PushCert(HttpContext.Session.GetUniacID(), files[0]);
                return JsonResponseModel.SuccessJson;
            }
            catch (Exception e)
            {
                e.Save();
                return JsonResponseModel.ErrorJson;
                throw;
            }
        }

    }
}
