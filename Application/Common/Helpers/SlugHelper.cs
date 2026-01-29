using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common.Helpers
{
    public static class SlugHelper
    {
        // Türkçe karakter dönüşüm tablosu
        private static readonly Dictionary<char, string> TurkishCharMap = new()
        {
            { 'ç', "c" }, { 'Ç', "c" },
            { 'ğ', "g" }, { 'Ğ', "g" },
            { 'ı', "i" }, { 'I', "i" },
            { 'İ', "i" }, { 'i', "i" },
            { 'ö', "o" }, { 'Ö', "o" },
            { 'ş', "s" }, { 'Ş', "s" },
            { 'ü', "u" }, { 'Ü', "u" }
        };

        /// <summary>
        /// Verilen metni SEO uyumlu slug'a çevirir
        /// Örnek: "Samsung Galaxy S24 Ultra 256GB" → "samsung-galaxy-s24-ultra-256gb"
        /// </summary>
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. Küçük harfe çevir
            var slug = text.ToLowerInvariant();

            // 2. Türkçe karakterleri dönüştür
            slug = ReplaceTurkishCharacters(slug);

            // 3. Diğer unicode karakterleri ASCII'ye çevir
            slug = RemoveDiacritics(slug);

            // 4. Sadece alfanumerik ve tire bırak
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // 5. Birden fazla boşluğu tek boşluğa çevir
            slug = Regex.Replace(slug, @"\s+", " ").Trim();

            // 6. Boşlukları tire ile değiştir
            slug = Regex.Replace(slug, @"\s", "-");

            // 7. Birden fazla tireyi tek tireye çevir
            slug = Regex.Replace(slug, @"-+", "-");

            // 8. Baş ve sondaki tireleri kaldır
            slug = slug.Trim('-');

            // 9. Maksimum 100 karakter
            if (slug.Length > 100)
            {
                slug = slug.Substring(0, 100).TrimEnd('-');
            }

            return slug;
        }

        /// <summary>
        /// Slug benzersiz olması için sonuna sayı ekler
        /// Örnek: "samsung-galaxy" → "samsung-galaxy-2"
        /// </summary>
        public static string MakeUnique(string slug, int counter)
        {
            if (counter <= 1)
                return slug;

            return $"{slug}-{counter}";
        }

        private static string ReplaceTurkishCharacters(string text)
        {
            var sb = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                if (TurkishCharMap.TryGetValue(c, out var replacement))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
