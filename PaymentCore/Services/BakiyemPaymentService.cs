using Flurl;
using Flurl.Http;
using Microsoft.Ajax.Utilities;
using Payment_project.PaymentCore.Models;
using System;
using System.Linq;
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
                settings = new Settings()
                {
                    AuthentionModel = CreateAuthention(DealerCode, Username, Password),
                    BaseUrl = BaseUrl,
                    Connected = false 
                };

  
                var testRequest = new DealerPaymentServicePaymentRequest
                {
                    PaymentDealerAuthentication = settings.AuthentionModel,
                    PaymentDealerRequest = new PaymentDealerRequest
                    {
                        DealerPaymentId = "1", 
                        OtherTrxCode = ""
                    }
                };

                string Url = settings.BaseUrl + "/PaymentDealer/DoDirectPaymentThreeD";

                var response = Url.WithHeader("Content-Type", "application/json")
                                 .PostJsonAsync(testRequest)
                                 .ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.StatusCode == 200)
                {
                  
                    var result = response.GetJsonAsync<DealerPaymentServicePaymentResult>()
                                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    settings.Connected = true;
                    return new ResultModel
                    {
                        IsSuccessful = true,
                        ErrorMessage = string.Empty
                    };
                }
                else
                {
                    settings.Connected = false; 
                    return new ResultModel
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"Servise ulaşılamadı. Status Code: {response.StatusCode}"
                    };
                }
            }
            catch (FlurlHttpException ex)
            {
              
                string errorDetail = "";
                try
                {
                    var errorResponse = ex.GetResponseStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    errorDetail = $" - API Response: {errorResponse}";
                }
                catch { }

                settings.Connected = false;
                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = $"HTTP Hatası: {ex.Message}{errorDetail}"
                };
            }
            catch (Exception Ex)
            {
                settings.Connected = false;
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
                    ErrorMessage = $"Ödeme işlemi başarısız. HTTP Status: {response.StatusCode}"
                };
            }
            catch (FlurlHttpException ex)
            {
              
                string errorDetail = "";
                try
                {
                    var errorResponse = ex.GetResponseStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    errorDetail = $" - API Yanıtı: {errorResponse}";
                }
                catch { }

                return new ResultModel
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Sunucu hatası: {ex.Message}{errorDetail}"
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

        #region Validation
        public ResultModel ValidatePaymentRequest(PaymentDealerRequest request) 
        {
            // Kart bilgileri kontrolü (CardToken varsa kart bilgileri opsiyonel)
            if (string.IsNullOrEmpty(request.CardToken))
            {
                if (string.IsNullOrEmpty(request.CardHolderFullName))
                    return new ResultModel { IsSuccessful = false, ErrorMessage = "Kart sahibinin adı gerekli" };

                if (string.IsNullOrEmpty(request.CardNumber) || request.CardNumber.Length < 13)
                    return new ResultModel { IsSuccessful = false, ErrorMessage = "Geçerli kart numarası gerekli" };

                if (string.IsNullOrEmpty(request.ExpMonth) || string.IsNullOrEmpty(request.ExpYear))
                    return new ResultModel { IsSuccessful = false, ErrorMessage = "Son kullanma tarihi gerekli" };

                if (string.IsNullOrEmpty(request.CvcNumber) || request.CvcNumber.Length < 3)
                    return new ResultModel { IsSuccessful = false, ErrorMessage = "Geçerli CVC kodu gerekli" };
            }

            // Zorunlu alanlar
            if (request.Amount <= 0)
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Tutar 0'dan büyük olmalı" };

            if (string.IsNullOrEmpty(request.Currency))
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Para birimi gerekli" };

            if (string.IsNullOrEmpty(request.RedirectUrl))
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Yönlendirme URL'si gerekli" };

            // İsteğe bağlı validasyonlar
            if (request.InstallmentNumber < 1 || request.InstallmentNumber > 9)
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Taksit sayısı 1 ile 9 arasında olmalı" };

            if (!string.IsNullOrEmpty(request.Software) && request.Software.Length > 30)
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Yazılım adı 30 karakteri geçemez" };

            if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 200)
                return new ResultModel { IsSuccessful = false, ErrorMessage = "Açıklama 200 karakteri geçemez" };

            // Currency kontrolü
            if (!string.IsNullOrEmpty(request.Currency))
            {
                var validCurrencies = new[] { "TL", "USD", "EUR", "GBP" };
                if (!validCurrencies.Contains(request.Currency.ToUpper()))
                    return new ResultModel { IsSuccessful = false, ErrorMessage = "Geçersiz para birimi. Geçerli para birimleri: TL, USD, EUR, GBP" };
            }

            return new ResultModel { IsSuccessful = true, ErrorMessage = string.Empty };
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

        public class PaymentDealerRequest
        {
            // Kart Bilgileri
            public string CardHolderFullName { get; set; }
            public string CardNumber { get; set; }
            public string ExpMonth { get; set; }
            public string ExpYear { get; set; }
            public string CvcNumber { get; set; }
            public string CardToken { get; set; } // Kart saklama için token

            // Ödeme Bilgileri
            public decimal Amount { get; set; }
            public string Currency { get; set; } = "TL";
            public int InstallmentNumber { get; set; } = 1;

            // Teknik Alanlar
            public string ClientIP { get; set; }
            public string RedirectUrl { get; set; }
            public int RedirectType { get; set; } = 0; // 0: Ana sayfa, 1: IFrame
            public string OtherTrxCode { get; set; } // Kendi işlem kodunuz

            // ✅ Eksik alan eklendi
            public string DealerPaymentId { get; set; }

            // Ödeme Türü
            public int IsPreAuth { get; set; } = 0; // 0: Doğrudan çekim, 1: Ön provizyon
            public int IsPoolPayment { get; set; } = 0; // 0: Normal, 1: Havuz ödeme

            // Entegrasyon Bilgileri
            public int? IntegratorId { get; set; } // Sistem entegratörü ID (opsiyonel)
            public string Software { get; set; } // Yazılım adı (max 30 karakter)
            public string SubMerchantName { get; set; } // Ekstrede görünecek isim
            public string Description { get; set; } // Açıklama (max 200 karakter)

            // Alıcı Bilgileri (opsiyonel)
            public BuyerInformation BuyerInformation { get; set; }
        }

        public class BuyerInformation
        {
            public string BuyerFullName { get; set; }
            public string BuyerEmail { get; set; }
            public string BuyerGsmNumber { get; set; }
            public string BuyerAddress { get; set; }
        }

        public class DealerPaymentServicePaymentResult
        {
            public string ResultCode { get; set; }
            public string ResultMessage { get; set; }
            public ThreeDData Data { get; set; } // ✅ string yerine object
            public string Exception { get; set; }
        }

        // ✅ Yeni model eklendi
        public class ThreeDData
        {
            public string Url { get; set; }
            public string CodeForHash { get; set; }
        }
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
            string hashKey = input; // dealerCode + "MK" + username + "PD" + password
            System.Text.Encoding encoding = Encoding.UTF8;
            byte[] plainBytes = encoding.GetBytes(hashKey);
            System.Security.Cryptography.SHA256Managed sha256Engine = new SHA256Managed();
            string hashedData = String.Empty;
            byte[] hashedBytes = sha256Engine.ComputeHash(plainBytes, 0, encoding.GetByteCount(hashKey));
            foreach (byte bit in hashedBytes)
            {
                hashedData += bit.ToString("x2");
            }
            sha256Engine.Dispose(); // Memory leak'i önlemek için
            return hashedData;
        }

        public static string GetUserFriendlyErrorMessage(string errorCode)
        {
            switch (errorCode)
            {
                // Authentication Errors
                case "PaymentDealer.CheckPaymentDealerAuthentication.InvalidRequest":
                    return "Hatalı hash bilgisi - Lütfen bayi bilgilerinizi kontrol edin.";

                case "PaymentDealer.CheckPaymentDealerAuthentication.InvalidAccount":
                    return "Böyle bir bayi bulunamadı - Bayi kodunuzu kontrol edin.";

                case "PaymentDealer.CheckPaymentDealerAuthentication.VirtualPosNotFound":
                    return "Bu bayi için sanal pos tanımı yapılmamış - Lütfen sistem yöneticinizle iletişime geçin.";

                // Limit Errors
                case "PaymentDealer.CheckDealerPaymentLimits.DailyDealerLimitExceeded":
                    return "Bayi limit aşımı nedeniyle işleminizi gerçekleştiremiyoruz. Lütfen ilgili birimimizle irtibata geçiniz.";

                case "PaymentDealer.CheckDealerPaymentLimits.DailyCardLimitExceeded":
                    return "Gün içinde bu kart kullanılarak daha fazla işlem yapılamaz.";

                // Card Errors
                case "PaymentDealer.CheckCardInfo.InvalidCardInfo":
                    return "Kart bilgilerinde hata var - Lütfen kart bilgilerinizi kontrol edin.";

                // 3D Secure Errors
                case "PaymentDealer.DoDirectPayment.ThreeDRequired":
                    return "3D Secure doğrulaması zorunlu.";

                // Installment Errors
                case "PaymentDealer.DoDirectPayment.InstallmentNotAvailableForForeignCurrencyTransaction":
                    return "Yabancı para işlemlerinde taksit işlemi uygulanamaz!";

                case "PaymentDealer.DoDirectPayment.ThisInstallmentNumberNotAvailableForDealer":
                    return "Seçtiğiniz taksit sayısı bayi hesabınızda tanımlı değildir!";

                case "PaymentDealer.DoDirectPayment.InvalidInstallmentNumber":
                    return "Taksit sayısı 2 ile 9 arasında olmalıdır.";

                case "PaymentDealer.DoDirectPayment.ThisInstallmentNumberNotAvailableForVirtualPos":
                    return "Bu taksit sayısı seçili sanal pos için kullanılamaz!";

                // General Error
                case "EX":
                    return "Beklenmeyen bir hata oluştu - Lütfen tekrar deneyiniz.";

                default:
                    return errorCode; // Return original error code if no friendly message available
            }
        }

        #endregion
    }
}