using System.Web.Mvc;
using Payment_project.PaymentCore.Helpers;
using Payment_project.PaymentCore.Models;
using PaymentCore.Services;
using System.Threading.Tasks;

namespace Payment_project.Controllers
{
    public class DealerPaymentController : Controller
    {
        private readonly BakiyemPaymentService _paymentService = new BakiyemPaymentService();

        [HttpGet]
        public ActionResult Pay3d()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Pay3d(PaymentDealerRequest model)
        {

            BakiyemPaymentService Client = new BakiyemPaymentService();
            if (!Client.StartClient("https://service.testmoka.com","","","").IsSuccessful) { return Json("Ödeme servisine bağlanılamadı!"); }




            var Response = Client.Start3DPaymentAsync();


            var dealerCode = "250";
            var username = "testuser";
            var password = "KLRSMGLSX";

            var checkKey = HashHelper.SHA256Hash(dealerCode + "MK" + username + "PD" + password);

            var request = new DealerPaymentServicePaymentRequest
            {
                PaymentDealerAuthentication = new PaymentDealerAuthentication
                {
                    DealerCode = dealerCode,
                    Username = username,
                    Password = password,
                    CheckKey = checkKey
                },
                PaymentDealerRequest = model
            };

            DealerPaymentServicePaymentResult result;
            try
            {
                result = await _paymentService.Start3DPaymentAsync(request);
            }
            catch
            {
                ViewBag.ErrorMessage = "Sunucuya bağlanırken bir hata oluştu.";
                return View();
            }

            if (result.ResultCode == "Success")
            {
                return Redirect(result.Data); // 3D ödeme sayfasına yönlendirme
            }

            ViewBag.ErrorMessage = result.ResultMessage;
            return View();
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
