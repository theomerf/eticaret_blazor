namespace Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendConfirmationEmailAsync(string email, string confirmationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task SendOrderCreatedEmailAsync(string email, string firstName, string orderNumber, decimal totalAmount, string currency);
        Task SendOrderShippedEmailAsync(string email, string firstName, string orderNumber, string trackingNumber, string? shippingCompanyName);
        Task SendOrderDeliveredEmailAsync(string email, string firstName, string orderNumber);
    }
}
