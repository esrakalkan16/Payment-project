using Flurl;
using Flurl.Http;
using Microsoft.Ajax.Utilities;
using Payment_project.PaymentCore.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace PaymentCore.Services
{
    public class BakiyemPaymentService
    {

        #region Static
        public Settings settings { get; set; } = new Settings();
        #endregion

        #region Operations
        public ResultModel StartClient(string BaseUrl, string DealerCode, string Username, string Password)
        {
            try
            {
                settings = new Settings() { AuthentionModel = CreateAuthention(DealerCode, Username, Password), BaseUrl = BaseUrl };


                var json = "{ " +
    "\"PaymentDealerAuthentication\": {" + settings.AuthentionModel + "}," +
    "\"PaymentDealerRequest\": {" +
        "\"DealerPaymentId\": \"1\"," +
        "\"OtherTrxCode\": \"\"" +
    "}" +
"}";

                string Url = settings.BaseUrl + "/PaymentDealer/GetDealerPaymentTrxDetailList";
                var response = Url.WithHeader("Content-Type", "application/json").PostStringAsync(json).ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.StatusCode == 200)
                {
                    return new ResultModel
                    {
                        IsSuccessful = true,
                        ErrorMessage = string.Empty
                    };
                }
                else
                {
                    settings.Connected = true;
                    return new ResultModel
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Servise ulaşılamadı"
                    };
                }
            }
            catch (Exception Ex)
            {
                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = Ex.Message
                };
            }
        }

        public ResultModel Start3DPaymentAsync(PaymentDealerRequest paymentRequest)
        {
            try
            {
                if (!settings.Connected)
                {
                    return new ResultModel
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Ödeme servisine bağlanılamadı!"
                    };
                }


                var requestBody = new DealerPaymentServicePaymentRequest
                {
                    PaymentDealerAuthentication = settings.AuthentionModel,
                    PaymentDealerRequest = paymentRequest
                };


                string url = settings.BaseUrl + "/PaymentDealer/DoDirectPaymentThreeD";
                var response = url
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(requestBody)
                    .ConfigureAwait(false).GetAwaiter().GetResult();


                if (response.StatusCode == 200)
                {
                    var result = response.GetJsonAsync<DealerPaymentServicePaymentResult>()
                                         .ConfigureAwait(false).GetAwaiter().GetResult();

                    return new ResultModel
                    {
                        IsSuccessful = result.ResultCode == "Success",
                        ErrorMessage = result.ResultMessage,
                        ResponseData = result
                    };
                }

                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = "Ödeme işlemi başarısız."
                };
            }
            catch (FlurlHttpException ex)
            {
                var error = ex.GetResponseJsonAsync<DealerPaymentServicePaymentResult>()
                              .ConfigureAwait(false).GetAwaiter().GetResult();

                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = error?.ResultMessage ?? "Sunucu hatası"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }



        #endregion

        #region Models
        public class Settings
        {
            public string BaseUrl { get; set; } = string.Empty;
            public bool Connected { get; set; } = false;
            public PaymentDealerAuthentication AuthentionModel { get; set; } = new PaymentDealerAuthentication();
        }

        public class ResultModel
        {
            public bool IsSuccessful { get; set; } = false;
            public string ErrorMessage { get; set; } = string.Empty;
            public object ResponseData { get; set; } = null;
        }


        #region Request
        public class PaymentDealerAuthentication
        {
            public string DealerCode { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string CheckKey { get; set; }
        }

        public class DealerPaymentServicePaymentRequest
        {
            public PaymentDealerAuthentication PaymentDealerAuthentication { get; set; }
            public PaymentDealerRequest PaymentDealerRequest { get; set; }
        }
        #endregion

        #region Response

        public class DealerPaymentServicePaymentResult
        {
            public string ResultCode { get; set; }
            public string ResultMessage { get; set; }
            public string Data { get; set; }
        }
        #endregion

        #endregion



        #region Helpers
        public PaymentDealerAuthentication CreateAuthention(string DealerCode, string Username, string Password)
        {
            return new PaymentDealerAuthentication()
            {
                DealerCode = DealerCode,
                Username = Username,
                Password = Password,
                CheckKey = SHA256Hash(DealerCode + "MK" + Username + "PD" + Password)
            };
        }
        public static string SHA256Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);

                var builder = new StringBuilder();
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
        #endregion

    }
}
