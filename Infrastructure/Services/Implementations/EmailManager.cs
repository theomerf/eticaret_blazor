using Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Infrastructure.Services.Implementations
{
    public class EmailManager : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _siteBaseUrl;
        private const string Brand = "SiparişKapıda";

        public EmailManager(IConfiguration configuration)
        {
            _smtpHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = configuration["EmailSettings:Username"] ?? "";
            _smtpPassword = configuration["EmailSettings:Password"] ?? "";
            _fromEmail = configuration["EmailSettings:FromEmail"] ?? "";
            _fromName = configuration["EmailSettings:FromName"] ?? Brand;
            _siteBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7175";
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(to);
                await client.SendMailAsync(mailMessage);

                Log.Information("Email sent successfully to {Email} with subject {Subject}", to, subject);
            }
            catch (SmtpException ex)
            {
                Log.Error(ex, "SMTP error while sending email to {Email}. Subject: {Subject}", to, subject);
                throw new InvalidOperationException($"Email gönderilemedi: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while sending email to {Email}. Subject: {Subject}", to, subject);
                throw new InvalidOperationException("Email gönderilirken beklenmeyen bir hata oluştu.", ex);
            }
        }

        public Task SendConfirmationEmailAsync(string email, string confirmationLink)
        {
            var body = BuildTemplate(
                preHeader: "Hesabınızı doğrulayın",
                title: "E-posta Adresinizi Doğrulayın",
                intro: "Üyeliğinizi tamamlamak için aşağıdaki butona tıklayın.",
                actionText: "E-postamı Doğrula",
                actionUrl: confirmationLink,
                detailLines:
                [
                    "Bu bağlantı 24 saat boyunca geçerlidir.",
                    "Eğer bu işlemi siz yapmadıysanız e-postayı güvenle yok sayabilirsiniz."
                ]);

            return SendAsync(email, "E-posta Adresinizi Doğrulayın", body);
        }

        public Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var body = BuildTemplate(
                preHeader: "Şifre sıfırlama talebi",
                title: "Şifrenizi Sıfırlayın",
                intro: "Şifre değişiklik talebi alındı. İşlemi onaylamak için aşağıdaki butonu kullanın.",
                actionText: "Şifremi Sıfırla",
                actionUrl: resetLink,
                detailLines:
                [
                    "Bu bağlantı 1 saat boyunca geçerlidir.",
                    "Bu işlemi siz yapmadıysanız şifrenizi değiştirmenizi öneririz."
                ]);

            return SendAsync(email, "Şifre Sıfırlama Talebi", body);
        }

        public Task SendWelcomeEmailAsync(string email, string firstName)
        {
            var body = BuildTemplate(
                preHeader: "Üyeliğiniz aktif",
                title: $"Hoş Geldiniz {firstName}",
                intro: "Hesabınız aktif. Kampanyaları inceleyip hemen alışverişe başlayabilirsiniz.",
                actionText: "Alışverişe Başla",
                actionUrl: _siteBaseUrl,
                detailLines:
                [
                    "Siparişlerinizi hesabınızdan anlık takip edebilirsiniz."
                ]);

            return SendAsync(email, "Hoş Geldiniz", body);
        }

        public Task SendOrderCreatedEmailAsync(string email, string firstName, string orderNumber, decimal totalAmount, string currency)
        {
            var body = BuildTemplate(
                preHeader: "Siparişiniz alındı",
                title: $"Siparişiniz Alındı: {orderNumber}",
                intro: $"{firstName}, ödeme süreciniz başlatıldı. Siparişiniz onaya alındı.",
                actionText: "Siparişlerim",
                actionUrl: $"{_siteBaseUrl}/account/orders",
                detailLines:
                [
                    $"Sipariş No: {orderNumber}",
                    $"Toplam Tutar: {FormatCurrency(totalAmount, currency)}"
                ]);

            return SendAsync(email, $"Sipariş Alındı - {orderNumber}", body);
        }

        public Task SendOrderShippedEmailAsync(string email, string firstName, string orderNumber, string trackingNumber, string? shippingCompanyName)
        {
            var shippingCompany = string.IsNullOrWhiteSpace(shippingCompanyName) ? "Kargo firması" : shippingCompanyName;

            var body = BuildTemplate(
                preHeader: "Siparişiniz kargoya verildi",
                title: $"Siparişiniz Yola Çıktı: {orderNumber}",
                intro: $"{firstName}, siparişiniz kargoya verildi.",
                actionText: "Sipariş Detayı",
                actionUrl: $"{_siteBaseUrl}/order/by-number/{orderNumber}",
                detailLines:
                [
                    $"Sipariş No: {orderNumber}",
                    $"Kargo: {shippingCompany}",
                    $"Takip No: {trackingNumber}"
                ]);

            return SendAsync(email, $"Sipariş Kargoda - {orderNumber}", body);
        }

        public Task SendOrderDeliveredEmailAsync(string email, string firstName, string orderNumber)
        {
            var body = BuildTemplate(
                preHeader: "Siparişiniz teslim edildi",
                title: $"Teslimat Tamamlandı: {orderNumber}",
                intro: $"{firstName}, siparişiniz başarıyla teslim edildi. Bizi tercih ettiğiniz için teşekkür ederiz.",
                actionText: "Siparişi Görüntüle",
                actionUrl: $"{_siteBaseUrl}/order/by-number/{orderNumber}",
                detailLines:
                [
                    $"Sipariş No: {orderNumber}",
                    "Dilerseniz ürün değerlendirmesi bırakabilirsiniz."
                ]);

            return SendAsync(email, $"Sipariş Teslim Edildi - {orderNumber}", body);
        }

        private static string FormatCurrency(decimal amount, string currency)
        {
            if (currency.Equals("TRY", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C2}", amount);
            }

            return $"{amount:N2} {currency}";
        }

        private static string BuildTemplate(
            string preHeader,
            string title,
            string intro,
            string actionText,
            string actionUrl,
            IEnumerable<string> detailLines)
        {
            var details = string.Join("", detailLines.Select(x => $"<li style='margin-bottom:8px;'>{x}</li>"));
            var safeActionUrl = WebUtility.HtmlEncode(actionUrl);

            return $"""
<!doctype html>
<html lang="tr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>{title}</title>
</head>
<body style="margin:0;background:#f3f6fb;font-family:'Segoe UI',Arial,sans-serif;color:#1f2937;">
  <span style="display:none;visibility:hidden;opacity:0;height:0;width:0;overflow:hidden;">{preHeader}</span>
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:32px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="640" cellspacing="0" cellpadding="0" style="max-width:640px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 16px 44px rgba(15,23,42,.12);">
          <tr>
            <td style="background:linear-gradient(135deg,#0ea5e9,#2563eb);padding:28px 32px;">
              <div style="font-size:12px;letter-spacing:.08em;color:#dbeafe;text-transform:uppercase;font-weight:700;">{Brand}</div>
              <h1 style="margin:8px 0 0 0;font-size:26px;line-height:1.25;color:#fff;">{title}</h1>
            </td>
          </tr>
          <tr>
            <td style="padding:28px 32px;">
              <p style="margin:0 0 16px 0;font-size:15px;line-height:1.7;color:#334155;">{intro}</p>
              <table role="presentation" cellspacing="0" cellpadding="0" style="margin:16px 0 22px 0;">
                <tr>
                  <td style="border-radius:10px;background:#0f172a;">
                    <a href="{safeActionUrl}" style="display:inline-block;padding:12px 20px;color:#fff;text-decoration:none;font-size:14px;font-weight:700;">{actionText}</a>
                  </td>
                </tr>
              </table>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:14px 16px;">
                <ul style="margin:0;padding-left:18px;color:#334155;font-size:14px;line-height:1.6;">
                  {details}
                </ul>
              </div>
              <p style="margin:16px 0 0 0;font-size:12px;line-height:1.6;color:#64748b;word-break:break-all;">Buton çalışmazsa bu adresi açın: {safeActionUrl}</p>
            </td>
          </tr>
          <tr>
            <td style="padding:18px 32px;background:#f8fafc;border-top:1px solid #e2e8f0;">
              <p style="margin:0;font-size:12px;color:#64748b;">Bu e-posta otomatik oluşturulmuştur. Lütfen yanıtlamayın.</p>
              <p style="margin:8px 0 0 0;font-size:12px;color:#64748b;">&copy; {DateTime.UtcNow.Year} {Brand}</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
        }
    }
}
