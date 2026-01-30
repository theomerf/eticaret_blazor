using Application.Common.Models;
using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly ISecurityLogService _securityLogService;
        private readonly IAuditLogService _auditLogService;
        private readonly ResiliencePipeline _paymentRetryPipeline;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IyzicoSettings _iyzicoSettings;
        private readonly HttpClient _httpClient;

        public PaymentService(
            ILogger<PaymentService> logger,
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

            // Resilience pipeline with retry and circuit breaker
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
                        _logger.LogError(
                            "Payment API circuit breaker opened due to high failure rate.");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation(
                            "Payment API circuit breaker closed, service recovered.");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private string GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        private string GetUserName() => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        private string GetIpAddress() => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        public async Task<OperationResult<PaymentResponse>> InitiatePaymentAsync(PaymentRequest request)
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();

            try
            {
                // Check for suspicious activity
                var recentAttempts = await _securityLogService.GetPaymentAttemptsFromIpAsync(ipAddress, TimeSpan.FromMinutes(15));
                if (recentAttempts > 10)
                {
                    await _securityLogService.LogRateLimitViolationAsync(userId, ipAddress, "payment-initiate", recentAttempts);
                    return OperationResult<PaymentResponse>.Failure(
                        "Çok fazla ödeme denemesi yapıldı. Lütfen daha sonra tekrar deneyiniz.",
                        ResultType.ValidationError);
                }

                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    if (_iyzicoSettings.UseMockResponses)
                    {
                        // Mock response for testing
                        return await CreateMockPaymentResponseAsync(request, userId, userName);
                    }
                    else
                    {
                        // Real Iyzico API call
                        return await CallIyzicoPaymentApiAsync(request, userId, userName);
                    }
                }, CancellationToken.None);

                return OperationResult<PaymentResponse>.Success(response);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Payment API circuit breaker is open. OrderNumber: {OrderNumber}", request.OrderNumber);
                return OperationResult<PaymentResponse>.Failure(
                    "Ödeme servisi şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyiniz.",
                    ResultType.ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation failed. OrderNumber: {OrderNumber}", request.OrderNumber);
                await _securityLogService.LogPaymentAnomalyAsync(userId, request.OrderNumber, "InitiationFailure", ex.Message);
                return OperationResult<PaymentResponse>.Failure(
                    "Ödeme başlatılamadı. Lütfen tekrar deneyiniz.",
                    ResultType.Error);
            }
        }

        private async Task<PaymentResponse> CreateMockPaymentResponseAsync(PaymentRequest request, string userId, string userName)
        {
            await Task.Delay(100); // Simulate API call

            var transactionId = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            _logger.LogInformation(
                "Payment initiated (MOCK). OrderNumber: {OrderNumber}, Amount: {Amount}, TransactionId: {TransactionId}",
                request.OrderNumber, request.Amount, transactionId);

            await _auditLogService.LogPaymentEventAsync(
                userId: userId,
                userName: userName,
                action: "PaymentInitiated",
                orderNumber: request.OrderNumber,
                transactionId: transactionId,
                amount: request.Amount,
                status: "Pending",
                provider: "MockGateway"
            );

            return new PaymentResponse
            {
                IsSuccess = true,
                TransactionId = transactionId,
                PaymentUrl = $"https://payment-gateway.example.com/pay/{transactionId}",
                Message = "Ödeme başlatıldı. Lütfen ödeme sayfasına yönlendiriliyorsunuz."
            };
        }

        private async Task<PaymentResponse> CallIyzicoPaymentApiAsync(PaymentRequest request, string userId, string userName)
        {
            var transactionId = $"IYZ-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}";

            // Build Iyzico payment request
            var iyzicoRequest = new
            {
                locale = "tr",
                conversationId = transactionId,
                price = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                paidPrice = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                currency = request.Currency,
                basketId = request.OrderNumber,
                paymentGroup = "PRODUCT",
                callbackUrl = _iyzicoSettings.CallbackUrl,
                enabledInstallments = new[] { 1, 2, 3, 6, 9 },
                buyer = new
                {
                    id = userId,
                    name = request.CustomerName?.Split(' ').FirstOrDefault() ?? "Customer",
                    surname = request.CustomerName?.Split(' ').Skip(1).FirstOrDefault() ?? "User",
                    email = request.CustomerEmail,
                    identityNumber = "11111111111", // In production, get from user profile
                    registrationAddress = "Address",
                    city = "Istanbul",
                    country = "Turkey"
                },
                shippingAddress = new
                {
                    contactName = request.CustomerName,
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Shipping Address"
                },
                billingAddress = new
                {
                    contactName = request.CustomerName,
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Billing Address"
                },
                basketItems = new[]
                {
                    new
                    {
                        id = "ITEM1",
                        name = "Order Items",
                        category1 = "General",
                        itemType = "PHYSICAL",
                        price = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(iyzicoRequest);
            var authString = GenerateIyzicoAuthString(jsonRequest);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/payment/iyzipos/checkoutform/initialize/auth/ecom")
            {
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", authString);
            httpRequest.Headers.Add("x-iyzi-rnd", Guid.NewGuid().ToString());

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Iyzico API call failed. Status: {StatusCode}, Response: {Response}",
                    httpResponse.StatusCode, responseContent);
                throw new Exception($"Iyzico API error: {httpResponse.StatusCode}");
            }

            var iyzicoResponse = JsonSerializer.Deserialize<IyzicoPaymentResponse>(responseContent);

            if (iyzicoResponse?.status != "success")
            {
                _logger.LogWarning("Iyzico payment initialization failed. Error: {Error}", iyzicoResponse?.errorMessage);
                throw new Exception($"Iyzico error: {iyzicoResponse?.errorMessage}");
            }

            _logger.LogInformation(
                "Payment initiated via Iyzico. OrderNumber: {OrderNumber}, Token: {Token}",
                request.OrderNumber, iyzicoResponse.token);

            await _auditLogService.LogPaymentEventAsync(
                userId: userId,
                userName: userName,
                action: "PaymentInitiated",
                orderNumber: request.OrderNumber,
                transactionId: iyzicoResponse.token,
                amount: request.Amount,
                status: "Pending",
                provider: "Iyzico"
            );

            return new PaymentResponse
            {
                IsSuccess = true,
                TransactionId = iyzicoResponse.token,
                PaymentUrl = iyzicoResponse.paymentPageUrl,
                Message = "Ödeme başlatıldı. Lütfen ödeme sayfasına yönlendiriliyorsunuz."
            };
        }

        private string GenerateIyzicoAuthString(string requestBody)
        {
            var randomString = Guid.NewGuid().ToString();
            var dataToHash = $"{_iyzicoSettings.ApiKey}{randomString}{_iyzicoSettings.SecretKey}{requestBody}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
            var hashString = Convert.ToBase64String(hashBytes);

            return $"IYZWS {_iyzicoSettings.ApiKey}:{hashString}";
        }

        public async Task<OperationResult<PaymentResponse>> VerifyPaymentAsync(string transactionId)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            try
            {
                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    if (_iyzicoSettings.UseMockResponses)
                    {
                        await Task.Delay(50, cancellationToken);

                        _logger.LogInformation("Payment verified (MOCK). TransactionId: {TransactionId}", transactionId);

                        return new PaymentResponse
                        {
                            IsSuccess = true,
                            TransactionId = transactionId,
                            Message = "Ödeme doğrulandı."
                        };
                    }
                    else
                    {
                        // Real Iyzico verification would go here
                        throw new NotImplementedException("Iyzico verification not yet implemented");
                    }
                }, CancellationToken.None);

                return OperationResult<PaymentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification failed. TransactionId: {TransactionId}", transactionId);
                return OperationResult<PaymentResponse>.Failure(
                    "Ödeme doğrulanamadı.",
                    ResultType.Error);
            }
        }

        public async Task<OperationResult<PaymentResponse>> RefundPaymentAsync(string transactionId, decimal amount)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            try
            {
                var response = await _paymentRetryPipeline.ExecuteAsync(async cancellationToken =>
                {
                    if (_iyzicoSettings.UseMockResponses)
                    {
                        await Task.Delay(100, cancellationToken);

                        var refundId = $"RFD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                        _logger.LogInformation(
                            "Refund initiated (MOCK). TransactionId: {TransactionId}, Amount: {Amount}, RefundId: {RefundId}",
                            transactionId, amount, refundId);

                        await _auditLogService.LogPaymentEventAsync(
                            userId: userId,
                            userName: userName,
                            action: "PaymentRefunded",
                            orderNumber: transactionId,
                            transactionId: refundId,
                            amount: amount,
                            status: "Refunded",
                            provider: "MockGateway"
                        );

                        return new PaymentResponse
                        {
                            IsSuccess = true,
                            TransactionId = refundId,
                            Message = "İade işlemi başlatıldı."
                        };
                    }
                    else
                    {
                        // Real Iyzico refund would go here
                        throw new NotImplementedException("Iyzico refund not yet implemented");
                    }
                }, CancellationToken.None);

                return OperationResult<PaymentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed. TransactionId: {TransactionId}, Amount: {Amount}", transactionId, amount);
                return OperationResult<PaymentResponse>.Failure(
                    "İade işlemi başarısız oldu.",
                    ResultType.Error);
            }
        }

        public async Task<OperationResult<bool>> ValidateCallbackAsync(PaymentCallbackDto callback)
        {
            var userId = GetUserId();
            var userName = GetUserName();

            try
            {
                // In production, verify the callback signature/hash from Iyzico
                _logger.LogInformation(
                    "Payment callback received. OrderNumber: {OrderNumber}, TransactionId: {TransactionId}, IsSuccess: {IsSuccess}",
                    callback.OrderNumber, callback.TransactionId, callback.IsSuccess);

                var action = callback.IsSuccess ? "PaymentCompleted" : "PaymentFailed";
                var status = callback.IsSuccess ? "Completed" : "Failed";

                await _auditLogService.LogPaymentEventAsync(
                    userId: userId,
                    userName: userName,
                    action: action,
                    orderNumber: callback.OrderNumber,
                    transactionId: callback.TransactionId,
                    amount: 0, // Amount should come from callback in production
                    status: status,
                    provider: callback.Provider
                );

                if (!callback.IsSuccess)
                {
                    await _securityLogService.LogPaymentAnomalyAsync(
                        userId, 
                        callback.OrderNumber, 
                        "PaymentFailed", 
                        callback.FailureReason ?? "Unknown reason"
                    );
                }

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment callback validation failed. OrderNumber: {OrderNumber}", callback.OrderNumber);
                return OperationResult<bool>.Failure(
                    "Callback doğrulanamadı.",
                    ResultType.Error);
            }
        }

        // Iyzico response model
        private class IyzicoPaymentResponse
        {
            public string status { get; set; } = string.Empty;
            public string? errorMessage { get; set; }
            public string token { get; set; } = string.Empty;
            public string paymentPageUrl { get; set; } = string.Empty;
        }
    }
}
