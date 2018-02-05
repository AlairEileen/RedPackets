using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedPackets.Managers
{
    public class RedPacketsAutoRefund
    {
        private static int hour = 0;
       private static Timer timer;
        public static void Start()
        {
            timer = new Timer(CheckRefund, null, 0, 1000);
        }

        private static void CheckRefund(object state)
        {
            throw new NotImplementedException();
        }
    }
}
