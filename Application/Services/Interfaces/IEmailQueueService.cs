namespace Application.Services.Interfaces
{
    public interface IEmailQueueService
    {
        string EnqueueConfirmationEmail(string email, string confirmationLink);
        string EnqueuePasswordResetEmail(string email, string resetLink);
        string EnqueueWelcomeEmail(string email, string firstName);
        string EnqueueOrderCreatedEmail(string email, string firstName, string orderNumber, decimal totalAmount, string currency);
        string EnqueueOrderShippedEmail(string email, string firstName, string orderNumber, string trackingNumber, string? shippingCompanyName);
        string EnqueueOrderDeliveredEmail(string email, string firstName, string orderNumber);
    }
}
