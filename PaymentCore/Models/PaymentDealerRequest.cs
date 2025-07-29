using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Payment_project.PaymentCore.Models
{
    public class PaymentDealerRequest
    {
        public string CardHolderFullName { get; set; }
        public string CardNumber { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
        public string CvcNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int InstallmentNumber { get; set; }
        public string ClientIP { get; set; }
        public string OtherTrxCode { get; set; }
        public string RedirectUrl { get; set; }
        public int RedirectType { get; set; }
        public int IsPreAuth { get; set; }
        public int IsPoolPayment { get; set; }
        public int? IntegratorId { get; set; }
        public string Software { get; set; }
        public string SubMerchantName { get; set; }
        public string Description { get; set; }
    }
}