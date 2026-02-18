using Application.Services.Interfaces;
using Hangfire;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Email;

namespace Infrastructure.BackgroundJobs.Hangfire
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public EmailQueueService(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string EnqueueConfirmationEmail(string email, string confirmationLink)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendConfirmationAsync(email, confirmationLink));

        public string EnqueuePasswordResetEmail(string email, string resetLink)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendPasswordResetAsync(email, resetLink));

        public string EnqueueWelcomeEmail(string email, string firstName)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendWelcomeAsync(email, firstName));

        public string EnqueueOrderCreatedEmail(string email, string firstName, string orderNumber, decimal totalAmount, string currency)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendOrderCreatedAsync(email, firstName, orderNumber, totalAmount, currency));

        public string EnqueueOrderShippedEmail(string email, string firstName, string orderNumber, string trackingNumber, string? shippingCompanyName)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendOrderShippedAsync(email, firstName, orderNumber, trackingNumber, shippingCompanyName));

        public string EnqueueOrderDeliveredEmail(string email, string firstName, string orderNumber)
            => _backgroundJobClient.Enqueue<EmailJob>(j => j.SendOrderDeliveredAsync(email, firstName, orderNumber));
    }
}
