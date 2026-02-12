using Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Application.DTOs;

namespace Infrastructure.Services.Implementations
{
    public class CaptchaManager : ICaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _secretKey;
        private readonly double _minimumScore;

        public CaptchaManager(IConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _httpContextAccessor = httpContextAccessor;
            _secretKey = _configuration["ReCaptcha:SecretKey"] ?? "";

            if (string.IsNullOrEmpty(_secretKey))
            {
                Log.Error("CRITICAL: ReCaptcha:SecretKey is missing in configuration! Captcha validation will fail.");
            }

            if (!double.TryParse(_configuration["ReCaptcha:MinimumScore"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _minimumScore))
            {
                _minimumScore = 0.5;
            }
        }

        public async Task<bool> ValidateAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Log.Error("CAPTCHA token is null or empty in ValidateAsync call.");
                return false;
            }

            var response = await VerifyAsync(token);
            
            if (!response.Success)
            {
                Log.Error("CAPTCHA validation failed. Errors: {Errors}", string.Join(", ", response.ErrorCodes));
                return false;
            }

            if (response.Score < _minimumScore)
            {
                Log.Error("CAPTCHA score too low: {Score} (Threshold: {MinScore})", response.Score, _minimumScore);
                return false;
            }

            Log.Information("CAPTCHA validated successfully. Score: {Score}", response.Score);
            return true;
        }

        public async Task<CaptchaResponseDto> VerifyAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new CaptchaResponseDto
                {
                    Success = false,
                    ErrorCodes = new[] { "missing-input-response" }
                };
            }

            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                var requestUrl = $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}";
                
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    requestUrl += $"&remoteip={ipAddress}";
                }

                var httpResponse = await _httpClient.PostAsync(requestUrl, null);
                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var captchaResponse = JsonSerializer.Deserialize<GoogleReCaptchaResponse>(jsonResponse, options);

                if (captchaResponse == null)
                {
                    Log.Error("Google ReCaptcha API returned null response.");
                    return new CaptchaResponseDto
                    {
                        Success = false,
                        ErrorCodes = new[] { "invalid-response" }
                    };
                }

                return new CaptchaResponseDto
                {
                    Success = captchaResponse.Success,
                    Score = captchaResponse.Score,
                    Action = captchaResponse.Action ?? "",
                    ChallengeTimestamp = captchaResponse.ChallengeTs,
                    Hostname = captchaResponse.Hostname ?? "",
                    ErrorCodes = captchaResponse.ErrorCodes ?? Array.Empty<string>()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error verifying CAPTCHA");
                return new CaptchaResponseDto
                {
                    Success = false,
                    ErrorCodes = new[] { "exception-occurred" }
                };
            }
        }

        // Google reCAPTCHA response model
        private class GoogleReCaptchaResponse
        {
            public bool Success { get; set; }
            public double Score { get; set; }
            public string? Action { get; set; }
            public DateTime ChallengeTs { get; set; }
            public string? Hostname { get; set; }
            public string[]? ErrorCodes { get; set; }
        }
    }
}
