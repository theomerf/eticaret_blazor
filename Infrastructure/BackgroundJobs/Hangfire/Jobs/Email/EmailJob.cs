using Application.Services.Interfaces;
using Hangfire;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Email
{
    public class EmailJob
    {
        private readonly IEmailService _emailService;

        public EmailJob(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [Queue(Queues.Default)]
        public Task SendConfirmationAsync(string email, string confirmationLink)
            => _emailService.SendConfirmationEmailAsync(email, confirmationLink);

        [Queue(Queues.Default)]
        public Task SendPasswordResetAsync(string email, string resetLink)
            => _emailService.SendPasswordResetEmailAsync(email, resetLink);

        [Queue(Queues.Default)]
        public Task SendWelcomeAsync(string email, string firstName)
            => _emailService.SendWelcomeEmailAsync(email, firstName);

        [Queue(Queues.Orders)]
        public Task SendOrderCreatedAsync(string email, string firstName, string orderNumber, decimal totalAmount, string currency)
            => _emailService.SendOrderCreatedEmailAsync(email, firstName, orderNumber, totalAmount, currency);

        [Queue(Queues.Orders)]
        public Task SendOrderShippedAsync(string email, string firstName, string orderNumber, string trackingNumber, string? shippingCompanyName)
            => _emailService.SendOrderShippedEmailAsync(email, firstName, orderNumber, trackingNumber, shippingCompanyName);

        [Queue(Queues.Orders)]
        public Task SendOrderDeliveredAsync(string email, string firstName, string orderNumber)
            => _emailService.SendOrderDeliveredEmailAsync(email, firstName, orderNumber);
    }
}
