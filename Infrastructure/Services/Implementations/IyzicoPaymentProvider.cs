using Application.Common.Models;
using Application.Common.Options;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Implementations
{
    public class IyzicoPaymentProvider : IPaymentProvider
    {
        private readonly ILogger<IyzicoPaymentProvider> _logger;
        private readonly ISecurityLogService _securityLogService;
        private readonly IAuditLogService _auditLogService;
        private readonly ResiliencePipeline _paymentRetryPipeline;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IyzicoSettings _iyzicoSettings;
        private readonly HttpClient _httpClient;

        public IyzicoPaymentProvider(
            ILogger<IyzicoPaymentProvider> logger,
            ISecurityLogService securityLogService,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<IyzicoSettings> iyzicoSettings,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _securityLogService = securityLogService;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _iyzicoSettings = iyzicoSettings.Value;
            _httpClient = httpClientFactory.CreateClient("Iyzico");
            _httpClient.BaseAddress = new Uri(_iyzicoSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_iyzicoSettings.TimeoutSeconds);

            _paymentRetryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Linear,
                    UseJitter = false,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Payment API call failed, retrying. Attempt: {AttemptNumber}, Exception: {Exception}",
                            args.AttemptNumber, args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    OnOpened = args =>
                    {
                        _logger.LogError("Payment API circuit breaker opened due to high failure rate.");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Payment API circuit breaker closed, service recovered.");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private string GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        private string GetUserName() => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        private string GetIpAddress() => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        #region Checkout Form Integration

        public async Task<OperationResult<IyzicoCheckoutFormInitResponse>> CreatePaymentAsync(IyzicoCheckoutFormInitRequest request)
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();

            try
            {
                var recentAttempts = await _securityLogService.GetPaymentAttemptsFromIpAsync(ipAddress, TimeSpan.FromMinutes(15));
                if (recentAttempts > 10)
                {
                    await _securityLogService.LogRateLimitViolationAsync(userId, ipAddress, "payment-initiate", recentAttempts);
                    return OperationResult<IyzicoCheckoutFormInitResponse>.Failure(
                        "Çok fazla ödeme denemesi yapıldı. Lütfen daha sonra tekrar deneyiniz.",
                        ResultType.ValidationError);
                }

                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    return await CallIyzicoCheckoutFormInitAsync(request, userId, userName);
                }, CancellationToken.None);

                return OperationResult<IyzicoCheckoutFormInitResponse>.Success(response);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Payment API circuit breaker is open. OrderNumber: {OrderNumber}", request.OrderNumber);

                return OperationResult<IyzicoCheckoutFormInitResponse>.Failure(
                    "Ödeme servisi şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyiniz.",
                    ResultType.ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation failed. OrderNumber: {OrderNumber}", request.OrderNumber);
                await _securityLogService.LogPaymentAnomalyAsync(userId, request.OrderNumber, "InitiationFailure", ex.Message);

                return OperationResult<IyzicoCheckoutFormInitResponse>.Failure(
                    "Ödeme başlatılamadı. Lütfen tekrar deneyiniz.",
                    ResultType.Error);
            }
        }

        public async Task<OperationResult<IyzicoCheckoutFormRetrieveResponse>> VerifyPaymentAsync(string token)
        {
            var userId = GetUserId();

            try
            {
                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    return await CallIyzicoCheckoutFormRetrieveAsync(token);
                }, CancellationToken.None);

                if (response == null || response.Status != "success" || response.PaymentStatus != "SUCCESS")
                {
                    _logger.LogWarning("Checkout form result retrieval failed. Token: {Token}, Error: {Error}",
                        token, response?.ErrorMessage);

                    await _securityLogService.LogPaymentAnomalyAsync(
                        userId, token, "CheckoutFormFailed", response?.ErrorMessage ?? "Unknown");

                    return OperationResult<IyzicoCheckoutFormRetrieveResponse>.Failure(
                        response?.ErrorMessage ?? "Ödeme sonucu alınamadı.",
                        ResultType.Error);
                }

                _logger.LogInformation(
                    "Checkout form result retrieved. PaymentId: {PaymentId}, Status: {Status}",
                    response.PaymentId, response.PaymentStatus);

                await _auditLogService.LogPaymentEventAsync(
                    userId: userId,
                    userName: GetUserName(),
                    action: response.PaymentStatus == "SUCCESS" ? "PaymentCompleted" : "PaymentFailed",
                    orderNumber: response.BasketId ?? "",
                    transactionId: response.PaymentId ?? "",
                    amount: response.PaidPrice,
                    status: response.PaymentStatus ?? "Unknown",
                    provider: "Iyzico"
                );

                return OperationResult<IyzicoCheckoutFormRetrieveResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout form result retrieval failed. Token: {Token}", token);
                return OperationResult<IyzicoCheckoutFormRetrieveResponse>.Failure(
                    "Ödeme sonucu alınamadı.",
                    ResultType.Error);
            }
        }

        public async Task<OperationResult<IyzicoRefundResponse>> RefundPaymentAsync(IyzicoRefundRequest request)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            try
            {
                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    return await CallIyzicoRefundAsync(request);
                }, CancellationToken.None);

                if (response == null || response.Status != "success")
                {
                    var errorMsg = response?.ErrorMessage ?? "Bilinmeyen hata";
                    _logger.LogWarning("Refund failed. PaymentTransactionId: {PaymentTransactionId}, Error: {Error}",
                        request.PaymentTransactionId, errorMsg);

                    await _securityLogService.LogPaymentAnomalyAsync(
                        userId: userId,
                        orderNumber: $"Refund-{request.PaymentTransactionId}",
                        anomalyType: "RefundFailure",
                        details: errorMsg
                    );

                    return OperationResult<IyzicoRefundResponse>.Failure(errorMsg, ResultType.Error);
                }

                await _auditLogService.LogPaymentTransactionAsync(
                    userId: userId,
                    userName: userName,
                    action: "PaymentRefunded",
                    orderNumber: $"Refund-{request.PaymentTransactionId}",
                    transactionId: response.PaymentTransactionId,
                    amount: request.Price,
                    status: "Refunded",
                    provider: "Iyzico"
                );

                _logger.LogInformation(
                    "Refund successful. PaymentTransactionId: {PaymentTransactionId}, RefundAmount: {Amount}",
                    response.PaymentTransactionId, response.Price);

                return OperationResult<IyzicoRefundResponse>.Success(response);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Payment API circuit breaker is open during refund. PaymentTransactionId: {PaymentTransactionId}",
                    request.PaymentTransactionId);
                return OperationResult<IyzicoRefundResponse>.Failure(
                    "İade servisi şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyiniz.",
                    ResultType.ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed. PaymentTransactionId: {PaymentTransactionId}",
                    request.PaymentTransactionId);
                return OperationResult<IyzicoRefundResponse>.Failure(
                    "İade işlemi başarısız oldu. Lütfen tekrar deneyiniz.",
                    ResultType.Error);
            }
        }

        public async Task<OperationResult<IyzicoBinCheckResponse>> GetBinDetailsAsync(string binNumber)
        {
            try
            {
                return await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    var request = new IyzicoBinCheckRequest
                    {
                        Locale = "tr",
                        ConversationId = Guid.NewGuid().ToString(),
                        BinNumber = binNumber
                    };

                    var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var httpRequest = CreateIyzicoRequest(HttpMethod.Post, "/payment/bin/check", jsonRequest);
                    var httpResponse = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Iyzico BIN Check API error. Status: {Status}, Response: {Response}",
                          httpResponse.StatusCode, responseContent);
                        return OperationResult<IyzicoBinCheckResponse>.Failure("BIN sorgulama başarısız.", ResultType.ServiceUnavailable);
                    }

                    var response = JsonSerializer.Deserialize<IyzicoBinCheckResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (response?.Status != "success")
                    {
                        return OperationResult<IyzicoBinCheckResponse>.Failure(response?.ErrorMessage ?? "Bilinmeyen hata", ResultType.Error);
                    }

                    return OperationResult<IyzicoBinCheckResponse>.Success(response);
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BIN check failed. BinNumber: {BinNumber}", binNumber);
                return OperationResult<IyzicoBinCheckResponse>.Failure("BIN sorgulama hatası.", ResultType.Error);
            }
        }


        #endregion

        #region Iyzico API Calls

        private async Task<IyzicoCheckoutFormInitResponse> CallIyzicoCheckoutFormInitAsync(IyzicoCheckoutFormInitRequest request, string userId, string userName)
        {
            var basketItems = new List<object>();

            foreach (var line in request.OrderLines)
            {
                basketItems.Add(new
                {
                    id = line.ProductId.ToString(),
                    price = line.LineTotal.ToString("F2", CultureInfo.InvariantCulture),
                    name = line.ProductName,
                    category1 = line.CategoryName,
                    category2 = line.SubCategoryName,
                    itemType = "PHYSICAL" // Veya "VIRTUAL" ürün türüne göre
                });
            }

            var iyzicoRequest = new
            {
                locale = "tr",
                conversationId = request.OrderNumber,
                price = request.SubTotal.ToString("F2", CultureInfo.InvariantCulture), // Sepettekilerin toplam tutarı
                paidPrice = request.TotalAmount.ToString("F2", CultureInfo.InvariantCulture), // Ödenecek toplam tutar (indirim, vergi vs. dahil)
                currency = request.Currency,
                basketId = request.OrderNumber,
                paymentGroup = "PRODUCT",
                callbackUrl = request.CallbackUrl,
                enabledInstallments = new[] { 1, 2, 3, 6, 9 },
                buyer = new
                {
                    id = userId,
                    name = request.BillingAddress.FirstName,
                    surname = request.BillingAddress.LastName,
                    identityNumber = request.CustomerIdentityNumber,
                    email = request.CustomerEmail,
                    gsmNumber = request.BillingAddress.Phone,
                    registrationAddress = string.Concat(request.BillingAddress.AddressLine, ", ", request.BillingAddress.District, ", ", request.BillingAddress.City),
                    city = request.BillingAddress.City,
                    country = "Turkey", // Değiştirilebilir
                    zipCode = request.BillingAddress.PostalCode,
                    ip = GetIpAddress()
                },
                shippingAddress = new
                {
                    address = string.Concat(request.BillingAddress.AddressLine, ", ", request.BillingAddress.District, ", ", request.BillingAddress.City),
                    zipCode = request.BillingAddress.PostalCode,
                    contactName = string.Concat(request.BillingAddress.FirstName, " ", request.BillingAddress.LastName),
                    city = request.BillingAddress.City,
                    country = "Turkey",
                },
                billingAddress = new
                {
                    address = string.Concat(request.BillingAddress.AddressLine, ", ", request.BillingAddress.District, ", ", request.BillingAddress.City),
                    zipCode = request.BillingAddress.PostalCode,
                    contactName = string.Concat(request.BillingAddress.FirstName, " ", request.BillingAddress.LastName),
                    city = request.BillingAddress.City,
                    country = "Turkey",
                },
                basketItems = basketItems
            };

            var jsonRequest = JsonSerializer.Serialize(iyzicoRequest);
            var httpRequest = CreateIyzicoRequest(HttpMethod.Post, "/payment/iyzipos/checkoutform/initialize/auth/ecom", jsonRequest);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("Iyzico Checkout Form Init Response: {Response}", responseContent);

            var response = JsonSerializer.Deserialize<IyzicoCheckoutFormInitResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response?.Status != "success")
            {
                _logger.LogWarning("Iyzico checkout form init failed. Error: {Error}", response?.ErrorMessage);
                throw new Exception($"Iyzico error: {response?.ErrorMessage ?? "Unknown error"}");
            }

            _logger.LogInformation(
                "Checkout form initialized. OrderNumber: {OrderNumber}, Token: {Token}",
                request.OrderNumber, response.Token);

            await _auditLogService.LogPaymentEventAsync(
                userId: userId,
                userName: userName,
                action: "PaymentInitiated",
                orderNumber: request.OrderNumber,
                transactionId: response.Token ?? "",
                amount: request.TotalAmount,
                status: "Pending",
                provider: "Iyzico"
            );

            return response;
        }

        private async Task<IyzicoCheckoutFormRetrieveResponse?> CallIyzicoCheckoutFormRetrieveAsync(string token)
        {
            var retrieveRequest = new
            {
                locale = "tr",
                conversationId = Guid.NewGuid().ToString(),
                token = token
            };

            var jsonRequest = JsonSerializer.Serialize(retrieveRequest);
            var httpRequest = CreateIyzicoRequest(HttpMethod.Post, "/payment/iyzipos/checkoutform/auth/ecom/detail", jsonRequest);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("Iyzico Checkout Form Retrieve Response: {Response}", responseContent);

            var response = JsonSerializer.Deserialize<IyzicoCheckoutFormRetrieveResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return response;
        }

        private HttpRequestMessage CreateIyzicoRequest(HttpMethod method, string path, string jsonBody)
        {
            var randomString = DateTime.Now.Ticks.ToString();

            var payload = string.IsNullOrEmpty(jsonBody)
                ? $"{randomString}{path}"
                : $"{randomString}{path}{jsonBody}";

            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_iyzicoSettings.SecretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            var authString = $"apiKey:{_iyzicoSettings.ApiKey}&randomKey:{randomString}&signature:{signature}";

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            var request = new HttpRequestMessage(method, path)
            {
                Content = !string.IsNullOrEmpty(jsonBody)
                    ? new StringContent(jsonBody, Encoding.UTF8, "application/json")
                    : null
            };

            request.Headers.Add("Authorization", $"IYZWSv2 {base64Auth}");
            request.Headers.Add("x-iyzi-client-version", "iyzipay-dotnet-v2");

            return request;
        }

        private async Task<IyzicoRefundResponse?> CallIyzicoRefundAsync(IyzicoRefundRequest request)
        {
            var refundRequest = new
            {
                locale = "tr",
                conversationId = Guid.NewGuid().ToString(),
                paymentTransactionId = request.PaymentTransactionId,
                price = request.Price.ToString("F2", CultureInfo.InvariantCulture),
                ip = request.Ip,
                currency = request.Currency,
                reason = request.Reason,
                description = request.Description
            };

            var jsonRequest = JsonSerializer.Serialize(refundRequest);
            var httpRequest = CreateIyzicoRequest(HttpMethod.Post, "/payment/refund", jsonRequest);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("Iyzico Refund Response: {Response}", responseContent);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Iyzico refund API error. Status: {Status}, Response: {Response}",
                    httpResponse.StatusCode, responseContent);
                return null;
            }

            var response = JsonSerializer.Deserialize<IyzicoRefundResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return response;
        }

        #endregion
    }
}