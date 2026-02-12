namespace Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendConfirmationEmailAsync(string email, string confirmationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string firstName);
    }
}
