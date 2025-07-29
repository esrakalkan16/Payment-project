using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Payment_project.PaymentCore.Models
{
    public class DealerPaymentServiceDirectPaymentResult
    {
        public DealerPaymentServiceDirectPaymentResultData Data { get; set; }
        public string ResultCode { get; set; }
        public string ResultMessage { get; set; }
        public string Exception { get; set; }
    }
}