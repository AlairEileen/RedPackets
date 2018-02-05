using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedPackets.Models
{
    public class ManageViewModel
    {
        public ProcessMiniInfo ProcessMiniInfo { get; set; }

        public QiNiuModel QiNiuModel { get; set; }
        public decimal ServiceRate { get; set; }
        public bool UploadedCert { get; set; }

        public bool IsRelease { get; set; }
    }
}
