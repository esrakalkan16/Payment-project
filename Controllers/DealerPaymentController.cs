using PaymentCore.Services;
using System;
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
                RedirectUrl = Request.Url.Scheme + "://" + Request.Url.Authority + "/DealerPayment/PayResult",
                DealerPaymentId = DateTime.Now.Ticks.ToString() 
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Pay3d(BakiyemPaymentService.PaymentDealerRequest model)
        {
            try
            {
                
                var connectionResult = _paymentService.StartClient(
                    BaseUrl: "https://service.refmokaunited.com",
                    DealerCode: "1731",
                    Username: "TestMoka2",
                    Password: "HYSYHDS8DU8HU"
                );

                if (!connectionResult.IsSuccessful)
                {
                    ViewBag.ErrorMessage = $"Bağlantı hatası: {connectionResult.ErrorMessage}";
                    return View(model);
                }

             
                if (string.IsNullOrEmpty(model.DealerPaymentId))
                    model.DealerPaymentId = DateTime.Now.Ticks.ToString();

                
                if (string.IsNullOrEmpty(model.ClientIP))
                    model.ClientIP = GetClientIP();

                if (string.IsNullOrEmpty(model.RedirectUrl))
                    model.RedirectUrl = Url.Action("PayResult", "DealerPayment", null, Request.Url.Scheme);

         
                var validationResult = _paymentService.ValidatePaymentRequest(model);
                if (!validationResult.IsSuccessful)
                {
                    ViewBag.ErrorMessage = $"Validasyon hatası: {validationResult.ErrorMessage}";
                    return View(model);
                }

             
                var result = _paymentService.Start3DPaymentAsync(model);

                if (result.IsSuccessful)
                {
                    var response = (BakiyemPaymentService.DealerPaymentServicePaymentResult)result.ResponseData;

                    if (response.ResultCode == "Success")
                    {
                     
                        return Redirect(response.Data.Url);
                    }
                    else
                    {
                       
                        string friendlyError = BakiyemPaymentService.GetUserFriendlyErrorMessage(response.ResultCode);
                        ViewBag.ErrorMessage = $"Ödeme başarısız: {friendlyError}";

                      
                        if (System.Web.HttpContext.Current.IsDebuggingEnabled)
                        {
                            ViewBag.ErrorMessage += $"<br/><small>Debug: {response.ResultCode} - {response.ResultMessage}</small>";
                        }

                        return View(model);
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = $"İşlem hatası: {result.ErrorMessage}";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Beklenmeyen hata: {ex.Message}";

                System.Diagnostics.Debug.WriteLine($"Payment Error: {ex}");

                return View(model);
            }
        }


        [HttpPost]
        public ActionResult PayResult()
        {
          
                try
                {
                    // Debug için tüm parametreleri logla
                    System.Diagnostics.Debug.WriteLine("=== PayResult Called ===");
                    System.Diagnostics.Debug.WriteLine("=== ALL FORM PARAMETERS ===");
                    foreach (string key in Request.Form.AllKeys ?? new string[0])
                    {
                        System.Diagnostics.Debug.WriteLine($"Form[{key}] = {Request.Form[key]}");
                    }
                    System.Diagnostics.Debug.WriteLine("=== ALL QUERY PARAMETERS ===");
                    foreach (string key in Request.QueryString.AllKeys ?? new string[0])
                    {
                        System.Diagnostics.Debug.WriteLine($"QueryString[{key}] = {Request.QueryString[key]}");
                    }

                    // Farklı parametre isimlerini kontrol et
                    var isSuccessful =
                        Request["isSuccessful"] == "true" || Request["isSuccessful"] == "True" ||
                        Request["IsSuccessful"] == "true" || Request["IsSuccessful"] == "True" ||
                        Request["success"] == "true" || Request["success"] == "True" ||
                        Request["Success"] == "true" || Request["Success"] == "True";

                    // TrxCode varsa ve başarısız bilgisi yoksa, başarılı kabul et
                    var trxCode = Request["trxCode"] ?? Request["TrxCode"] ?? Request["transactionCode"] ?? "";
                    if (!string.IsNullOrEmpty(trxCode) && string.IsNullOrEmpty(Request["isSuccessful"]))
                    {
                        isSuccessful = true; // TrxCode varsa başarılı kabul et
                    }

                    var resultCode = Request["resultCode"] ?? Request["ResultCode"] ?? Request["errorCode"] ?? "";
                    var resultMessage = Request["resultMessage"] ?? Request["ResultMessage"] ?? Request["message"] ?? "";
                    var otherTrxCode = Request["otherTrxCode"] ?? Request["OtherTrxCode"] ?? "";
                    var amount = Request["amount"] ?? Request["Amount"] ?? "";
                    var currency = Request["currency"] ?? Request["Currency"] ?? "";

                    // ViewBag'e değerleri aktar
                    ViewBag.IsSuccessful = isSuccessful;
                    ViewBag.ResultCode = resultCode;
                    ViewBag.ResultMessage = resultMessage;
                    ViewBag.TrxCode = trxCode;
                    ViewBag.OtherTrxCode = otherTrxCode;
                    ViewBag.Amount = amount;
                    ViewBag.Currency = currency;

                    // CSS class ve mesajı belirle
                    ViewBag.MessageClass = isSuccessful ? "success-message" : "error-message";

                    if (isSuccessful)
                    {
                        ViewBag.Title = "Ödeme Başarılı";
                        ViewBag.Message = $"✅ Ödemeniz başarıyla tamamlandı!";
                        ViewBag.Details = $"İşlem Kodu: {trxCode}";

                        if (!string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(currency))
                        {
                            ViewBag.Details += $"<br/>Tutar: {amount} {currency}";
                        }
                    }
                    else
                    {
                        ViewBag.Title = "Ödeme Başarısız";

                        if (!string.IsNullOrEmpty(resultMessage))
                        {
                            ViewBag.Message = $"❌ {resultMessage}";
                        }
                        else if (!string.IsNullOrEmpty(resultCode))
                        {
                            string friendlyError = BakiyemPaymentService.GetUserFriendlyErrorMessage(resultCode);
                            ViewBag.Message = $"❌ {friendlyError}";
                        }
                        else
                        {
                            ViewBag.Message = "❌ Ödeme işlemi tamamlanamadı.";
                        }

                        // Debug bilgisi (sadece geliştirme ortamında)
                        if (System.Web.HttpContext.Current.IsDebuggingEnabled)
                        {
                            ViewBag.Details = $"Hata Kodu: {resultCode}<br/>Detay: {resultMessage}";
                        }
                    }

                    return View();
                }
                catch (Exception ex)
                {
                    ViewBag.IsSuccessful = false;
                    ViewBag.Title = "Hata";
                    ViewBag.Message = "❌ Sonuç sayfası yüklenirken bir hata oluştu.";
                    ViewBag.MessageClass = "error-message";
                    ViewBag.Details = $"Debug: {ex.Message}";

                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"PayResult Error: {ex}");

                    return View();
                }
            }

        private string GetClientIP()
        {
            string ipAddress = "";

            // Proxy arkasındaysak
            if (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                // Birden fazla IP varsa ilkini al
                if (ipAddress.Contains(","))
                    ipAddress = ipAddress.Split(',')[0].Trim();
            }
            else if (Request.ServerVariables["HTTP_X_REAL_IP"] != null)
                ipAddress = Request.ServerVariables["HTTP_X_REAL_IP"];
            else if (Request.ServerVariables["HTTP_CLIENT_IP"] != null)
                ipAddress = Request.ServerVariables["HTTP_CLIENT_IP"];
            else if (Request.ServerVariables["REMOTE_ADDR"] != null)
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            else if (Request.UserHostAddress != null)
                ipAddress = Request.UserHostAddress;

            // IPv6 localhost'u IPv4'e çevir
            if (ipAddress == "::1")
                ipAddress = "127.0.0.1";

            // Boşsa default IP ver
            if (string.IsNullOrEmpty(ipAddress))
                ipAddress = "127.0.0.1";

            return ipAddress;
        }

        // ✅ Yardımcı metod: Test için örnek kart bilgileri
        [HttpGet]
        public JsonResult GetTestCardInfo()
        {
            if (!System.Web.HttpContext.Current.IsDebuggingEnabled)
            {
                return Json(new { error = "Bu özellik sadece geliştirme ortamında kullanılabilir." }, JsonRequestBehavior.AllowGet);
            }

            var testCard = new
            {
                CardHolderFullName = "Test User",
                CardNumber = "5555666677778888",
                ExpMonth = "12",
                ExpYear = "2025",
                CvcNumber = "123",
                Amount = 1.00m
            };

            return Json(testCard, JsonRequestBehavior.AllowGet);
        }
    }
}