using Application.Common.Security;
using Ganss.Xss;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security
{
    public class HtmlSanitizerManager : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;
        private readonly ILogger<HtmlSanitizerManager> _logger;

        public HtmlSanitizerManager(ILogger<HtmlSanitizerManager> logger)
        {
            _logger = logger;
            _sanitizer = new HtmlSanitizer();

            ConfigureSanitizer();
        }

        private void ConfigureSanitizer()
        {
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("s");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("h4");
            _sanitizer.AllowedTags.Add("h5");
            _sanitizer.AllowedTags.Add("h6");
            _sanitizer.AllowedTags.Add("blockquote");
            _sanitizer.AllowedTags.Add("a");
            _sanitizer.AllowedTags.Add("img");
            _sanitizer.AllowedTags.Add("table");
            _sanitizer.AllowedTags.Add("thead");
            _sanitizer.AllowedTags.Add("tbody");
            _sanitizer.AllowedTags.Add("tr");
            _sanitizer.AllowedTags.Add("th");
            _sanitizer.AllowedTags.Add("td");
            _sanitizer.AllowedTags.Add("div");
            _sanitizer.AllowedTags.Add("span");
            _sanitizer.AllowedTags.Add("hr");

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("src"); 
            _sanitizer.AllowedAttributes.Add("alt");
            _sanitizer.AllowedAttributes.Add("title");
            _sanitizer.AllowedAttributes.Add("class");
            _sanitizer.AllowedAttributes.Add("id");
            _sanitizer.AllowedAttributes.Add("style");
            _sanitizer.AllowedAttributes.Add("target");
            _sanitizer.AllowedAttributes.Add("rel");
            _sanitizer.AllowedAttributes.Add("width");
            _sanitizer.AllowedAttributes.Add("height");
            _sanitizer.AllowedAttributes.Add("align");
            _sanitizer.AllowedAttributes.Add("colspan");
            _sanitizer.AllowedAttributes.Add("rowspan");

            _sanitizer.AllowedCssProperties.Clear();
            _sanitizer.AllowedCssProperties.Add("color");
            _sanitizer.AllowedCssProperties.Add("background-color");
            _sanitizer.AllowedCssProperties.Add("font-size");
            _sanitizer.AllowedCssProperties.Add("font-weight");
            _sanitizer.AllowedCssProperties.Add("font-style");
            _sanitizer.AllowedCssProperties.Add("text-align");
            _sanitizer.AllowedCssProperties.Add("text-decoration");
            _sanitizer.AllowedCssProperties.Add("margin");
            _sanitizer.AllowedCssProperties.Add("padding");
            _sanitizer.AllowedCssProperties.Add("border");
            _sanitizer.AllowedCssProperties.Add("width");
            _sanitizer.AllowedCssProperties.Add("height");
            _sanitizer.AllowedCssProperties.Add("display");

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");

            _sanitizer.KeepChildNodes = true; 
            _sanitizer.AllowDataAttributes = false;
        }

        public string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            try
            {
                var sanitized = _sanitizer.Sanitize(html);

                if (sanitized != html)
                {
                    _logger.LogWarning("HTML sanitized. Original length: {OriginalLength}, Sanitized length: {SanitizedLength}",
                        html.Length, sanitized.Length);
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing HTML. Returning empty string.");
                return string.Empty;
            }
        }

        public string SanitizeToPlainText(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            try
            {
                var tempSanitizer = new HtmlSanitizer();
                tempSanitizer.AllowedTags.Clear();

                var plainText = tempSanitizer.Sanitize(html);

                return plainText.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting HTML to plain text. Returning empty string.");
                return string.Empty;
            }
        }
    }
}
