using Application.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Infrastructure.Services.Implementations
{
    public class EmailManager : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailManager(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:Username"] ?? "";
            _smtpPassword = _configuration["EmailSettings:Password"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "";
            _fromName = _configuration["EmailSettings:FromName"] ?? "SiparişKapıda";
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
                    IsBodyHtml = true
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

        public async Task SendConfirmationEmailAsync(string email, string confirmationLink)
        {
            var subject = "E-posta Adresinizi Doğrulayın";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Hoş Geldiniz!</h1>
                        </div>
                        <div class='content'>
                            <p>Merhaba,</p>
                            <p>E-Ticaret platformumuza kaydolduğunuz için teşekkür ederiz!</p>
                            <p>Hesabınızı aktif etmek için lütfen aşağıdaki butona tıklayın:</p>
                            <p style='text-align: center;'>
                                <a href='{confirmationLink}' class='button'>E-postamı Doğrula</a>
                            </p>
                            <p>Veya aşağıdaki linki tarayıcınıza kopyalayın:</p>
                            <p style='word-break: break-all; font-size: 12px;'>{confirmationLink}</p>
                            <p><strong>Not:</strong> Bu link 24 saat geçerlidir.</p>
                        </div>
                        <div class='footer'>
                            <p>Bu e-postayı siz talep etmediyseniz, lütfen görmezden gelin.</p>
                            <p>&copy; 2026 E-Ticaret. Tüm hakları saklıdır.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            try
            {
                await SendAsync(email, subject, body);
                Log.Information("Email confirmation sent to {Email}", email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send email confirmation to {Email}", email);
                throw; // Controller'a exception fırlat
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Şifre Sıfırlama Talebi";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #FF5722; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #FF5722; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Şifre Sıfırlama</h1>
                        </div>
                        <div class='content'>
                            <p>Merhaba,</p>
                            <p>Şifrenizi sıfırlamak için bir talepte bulundunuz.</p>
                            <p>Şifrenizi sıfırlamak için lütfen aşağıdaki butona tıklayın:</p>
                            <p style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
                            </p>
                            <p>Veya aşağıdaki linki tarayıcınıza kopyalayın:</p>
                            <p style='word-break: break-all; font-size: 12px;'>{resetLink}</p>
                            <p><strong>Not:</strong> Bu link 1 saat geçerlidir.</p>
                            <p><strong>Güvenlik Uyarısı:</strong> Bu talebi siz yapmadıysanız, lütfen bu e-postayı görmezden gelin ve şifrenizi değiştirin.</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2026 E-Ticaret. Tüm hakları saklıdır.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            try
            {
                await SendAsync(email, subject, body);
                Log.Information("Password reset email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send password reset email to {Email}", email);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string firstName)
        {
            var subject = "Hoş Geldiniz!";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ display: inline-block; padding: 12px 24px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Hoş Geldiniz {firstName}!</h1>
                        </div>
                        <div class='content'>
                            <p>Merhaba {firstName},</p>
                            <p>E-Ticaret ailesine katıldığınız için çok mutluyuz!</p>
                            <p>Hesabınız başarıyla oluşturuldu ve artık alışverişe başlayabilirsiniz.</p>
                            <p style='text-align: center;'>
                                <a href='https://localhost:7175' class='button'>Alışverişe Başla</a>
                            </p>
                            <p>İyi alışverişler dileriz!</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2026 E-Ticaret. Tüm hakları saklıdır.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            try
            {
                await SendAsync(email, subject, body);
                Log.Information("Welcome email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send welcome email to {Email}", email);
                throw;
            }
        }
    }
}
