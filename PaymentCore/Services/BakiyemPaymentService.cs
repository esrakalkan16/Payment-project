using Flurl.Http;
using Payment_project.PaymentCore.Models;
using System.Threading.Tasks;

namespace PaymentCore.Services
{
    public class BakiyemPaymentService
    {
        private const string BaseUrl = "https://service.testmoka.com";

        public async Task<DealerPaymentServicePaymentResult> Start3DPaymentAsync(DealerPaymentServicePaymentRequest request)
        {
            string url = $"{BaseUrl}/PaymentDealer/DoDirectPaymentThreeD";
            var response = await url
                .PostJsonAsync(request)
                .ReceiveJson<DealerPaymentServicePaymentResult>();

            return response;
        }
    }
}
