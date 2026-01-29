namespace Application.Common.Security
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string? html);
        string SanitizeToPlainText(string? html);
    }
}
