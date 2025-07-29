using Flurl;
using Flurl.Http;
using Payment_project.PaymentCore.Models;
using System;
using System.Threading.Tasks;

namespace PaymentCore.Services
{
    public class BakiyemPaymentService
    {
        private readonly string _baseUrl;
        private readonly PaymentDealerAuthentication _authentication;
        private readonly IFlurlClient _flurlClient;

        public BakiyemPaymentService(string baseUrl = "https://service.testmoka.com")
        {
            _baseUrl = baseUrl;
            _flurlClient = new FlurlClient(_baseUrl)
                .WithTimeout(TimeSpan.FromSeconds(30))
                .WithHeader("Accept", "application/json");
        }

        public void SetAuthentication(PaymentDealerAuthentication authentication)
        {
            _authentication = authentication;
        }

        public async Task<DealerPaymentServicePaymentResult> Start3DPaymentAsync(PaymentDealerRequest paymentRequest)
        {
            var request = new DealerPaymentServicePaymentRequest
            {
                PaymentDealerAuthentication = _authentication,
                PaymentDealerRequest = paymentRequest
            };

            try
            {
                return await _flurlClient
                    .Request("/PaymentDealer/DoDirectPaymentThreeD")
                    .PostJsonAsync(request)
                    .ReceiveJson<DealerPaymentServicePaymentResult>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<DealerPaymentServicePaymentResult>();
                if (error != null)
                {
                    return error;
                }
                throw;
            }
        }

        public async Task<DealerPaymentServiceDirectPaymentResult> DirectPaymentAsync(PaymentDealerRequest paymentRequest)
        {
            var request = new DealerPaymentServicePaymentRequest
            {
                PaymentDealerAuthentication = _authentication,
                PaymentDealerRequest = paymentRequest
            };

            try
            {
                return await _flurlClient
                    .Request("/PaymentDealer/DoDirectPayment")
                    .PostJsonAsync(request)
                    .ReceiveJson<DealerPaymentServiceDirectPaymentResult>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<DealerPaymentServiceDirectPaymentResult>();
                if (error != null)
                {
                    return error;
                }
                throw;
            }
        }

        public async Task<DealerPaymentServicePaymentResult> GetPaymentStatusAsync(string trxCode)
        {
            try
            {
                return await _flurlClient
                    .Request("/PaymentDealer/GetPaymentStatus")
                    .SetQueryParam("trxCode", trxCode)
                    .GetJsonAsync<DealerPaymentServicePaymentResult>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<DealerPaymentServicePaymentResult>();
                if (error != null)
                {
                    return error;
                }
                throw;
            }
        }
    }
}
