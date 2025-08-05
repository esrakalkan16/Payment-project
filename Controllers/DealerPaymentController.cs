using PaymentCore.Services;
using System.Web.Mvc;

namespace Payment_project.Controllers
{
    public class DealerPaymentController : Controller
    {
        private readonly BakiyemPaymentService _paymentService;

        public DealerPaymentController()
        {
            _paymentService = new BakiyemPaymentService();
        }

        [HttpGet]
        public ActionResult Pay3d()
        {
            var model = new BakiyemPaymentService.PaymentDealerRequest
            {
                Currency = "TL",
                InstallmentNumber = 1,
                ClientIP = GetClientIP(),
                RedirectUrl = Url.Action("PayResult", "DealerPayment", null, Request.Url.Scheme)
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Pay3d(BakiyemPaymentService.PaymentDealerRequest model)
        {
           

            // Bağlantıyı başlat
            var connectionResult = _paymentService.StartClient(
                BaseUrl: "https://service.testmoka.com",
                DealerCode: "1731",
                Username: "TestMoka2",
                Password: "HYSYHDS8DU8HU"
            );

            if (!connectionResult.IsSuccessful)
            {
                ViewBag.ErrorMessage = $"Bağlantı hatası: {connectionResult.ErrorMessage}";
                return View(model);
            }

            // Gerekli alanlar eksikse tamamla
            if (string.IsNullOrEmpty(model.ClientIP))
                model.ClientIP = GetClientIP();

            if (string.IsNullOrEmpty(model.RedirectUrl))
                model.RedirectUrl = Url.Action("PayResult", "DealerPayment", null, Request.Url.Scheme);

            // Ödemeyi başlat
            var result = _paymentService.Start3DPaymentAsync(model);

            if (result.IsSuccessful)
            {
                var response = (BakiyemPaymentService.DealerPaymentServicePaymentResult)result.ResponseData;

                if (response.ResultCode == "Success")
                {
                    return Redirect(response.Data); // 3D ödeme yönlendirmesi
                }

                ViewBag.ErrorMessage = $"Ödeme başarısız: {response.ResultMessage}";
                return View(model);
            }

            ViewBag.ErrorMessage = $"İşlem hatası: {result.ErrorMessage}";
            return View(model);
        }

        [HttpGet]
        [HttpPost]
        public ActionResult PayResult()
        {
            var isSuccessful = Request["isSuccessful"] == "true" || Request["isSuccessful"] == "True";
            var resultCode = Request["resultCode"] ?? "";
            var resultMessage = Request["resultMessage"] ?? "";
            var trxCode = Request["trxCode"] ?? "";
            var otherTrxCode = Request["otherTrxCode"] ?? "";

            ViewBag.IsSuccessful = isSuccessful;
            ViewBag.ResultCode = resultCode;
            ViewBag.ResultMessage = resultMessage;
            ViewBag.TrxCode = trxCode;
            ViewBag.OtherTrxCode = otherTrxCode;

            ViewBag.MessageClass = isSuccessful ? "success-message" : "error-message";
            ViewBag.Message = isSuccessful
                ? $"✅ Ödeme başarılı! İşlem Kodu: {trxCode}"
                : $"❌ Ödeme başarısız: {resultCode} - {resultMessage}";

            return View();
        }

        private string GetClientIP()
        {
            string ipAddress = "";

            if (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            else if (Request.ServerVariables["HTTP_X_REAL_IP"] != null)
                ipAddress = Request.ServerVariables["HTTP_X_REAL_IP"];
            else if (Request.ServerVariables["REMOTE_ADDR"] != null)
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            else if (Request.UserHostAddress != null)
                ipAddress = Request.UserHostAddress;

            if (ipAddress == "::1")
                ipAddress = "127.0.0.1";

            return string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress;
        }
    }
}
