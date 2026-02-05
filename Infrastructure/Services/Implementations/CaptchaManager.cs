using Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

namespace Infrastructure.Services.Implementations
{
    public class CaptchaManager : ICaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly double _minimumScore;

        public CaptchaManager(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _secretKey = _configuration["ReCaptcha:SecretKey"] ?? "";
            _minimumScore = double.Parse(_configuration["ReCaptcha:MinimumScore"] ?? "0.5");
        }

        public async Task<bool> ValidateAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Log.Warning("CAPTCHA token is null or empty");
                return false;
            }

            var response = await VerifyAsync(token);
            
            if (!response.Success)
            {
                Log.Warning("CAPTCHA validation failed. Errors: {Errors}", string.Join(", ", response.ErrorCodes));
                return false;
            }

            if (response.Score < _minimumScore)
            {
                Log.Warning("CAPTCHA score too low: {Score}", response.Score);
                return false;
            }

            Log.Information("CAPTCHA validated successfully. Score: {Score}", response.Score);
            return true;
        }

        public async Task<CaptchaResponse> VerifyAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new CaptchaResponse
                {
                    Success = false,
                    ErrorCodes = new[] { "missing-input-response" }
                };
            }

            try
            {
                var requestUrl = $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}";
                var httpResponse = await _httpClient.PostAsync(requestUrl, null);
                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var captchaResponse = JsonSerializer.Deserialize<GoogleReCaptchaResponse>(jsonResponse, options);

                if (captchaResponse == null)
                {
                    return new CaptchaResponse
                    {
                        Success = false,
                        ErrorCodes = new[] { "invalid-response" }
                    };
                }

                return new CaptchaResponse
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
                return new CaptchaResponse
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
