using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Payment_project.PaymentCore.Models
{
    public class PaymentDealerAuthentication
    {
        public string DealerCode { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CheckKey { get; set; }
    }
}