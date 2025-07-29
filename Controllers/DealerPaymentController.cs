using System.Web.Mvc;
using Payment_project.PaymentCore.Helpers;
using Payment_project.PaymentCore.Models;
using PaymentCore.Services;
using System.Threading.Tasks;
using System;

namespace Payment_project.Controllers
{
    public class DealerPaymentController : Controller
    {
        private readonly BakiyemPaymentService _paymentService;
        private const string DealerCode = "250";
        private const string Username = "testuser";
        private const string Password = "KLRSMGLSX";

        public DealerPaymentController()
        {
            _paymentService = new BakiyemPaymentService();
            InitializeAuthentication();
        }

        private void InitializeAuthentication()
        {
            var checkKey = HashHelper.SHA256Hash(DealerCode + "MK" + Username + "PD" + Password);
            var authentication = new PaymentDealerAuthentication
            {
                DealerCode = DealerCode,
                Username = Username,
                Password = Password,
                CheckKey = checkKey
            };
            _paymentService.SetAuthentication(authentication);
        }

        [HttpGet]
        public ActionResult Pay3d()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Pay3d(PaymentDealerRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.ClientIP = Request.UserHostAddress;
                model.RedirectUrl = Url.Action("PayResult", "DealerPayment", null, Request.Url.Scheme);
                
                var result = await _paymentService.Start3DPaymentAsync(model);

                if (result.ResultCode == "Success")
                {
                    return Redirect(result.Data); // 3D ödeme sayfasına yönlendirme
                }

                ViewBag.ErrorMessage = result.ResultMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ödeme işlemi sırasında bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<ActionResult> DirectPayment(PaymentDealerRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Geçersiz ödeme bilgileri" });
                }

                model.ClientIP = Request.UserHostAddress;
                
                var result = await _paymentService.DirectPaymentAsync(model);

                return Json(new { 
                    success = result.ResultCode == "Success",
                    message = result.ResultMessage,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ödeme işlemi sırasında bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> CheckPaymentStatus(string trxCode)
        {
            try
            {
                var result = await _paymentService.GetPaymentStatusAsync(trxCode);
                return Json(new { 
                    success = result.ResultCode == "Success",
                    message = result.ResultMessage,
                    data = result.Data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ödeme durumu kontrol edilirken bir hata oluştu: " + ex.Message }, 
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult PayResult(bool isSuccessful, string resultCode, string resultMessage, string trxCode)
        {
            if (isSuccessful)
                ViewBag.Message = "✅ Ödeme başarılı. Trx: " + trxCode;
            else
                ViewBag.Message = "❌ Ödeme başarısız: " + resultCode + " - " + resultMessage;

            return View();
        }
    }
}
