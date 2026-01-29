namespace Application.Common.Helpers
{
    public static class SeoMetaHelper
    {
        private const int MaxTitleLength = 60;
        private const int MaxDescriptionLength = 160;

        public static string GenerateProductMetaTitle(string productName, string? brandName = null, string siteName = "E-Ticaret")
        {
            if (string.IsNullOrWhiteSpace(productName))
                return siteName;

            string title;

            if (!string.IsNullOrWhiteSpace(brandName))
            {
                title = $"{productName} - {brandName}";
            }
            else
            {
                title = productName;
            }

            // Site adını ekle
            var withSite = $"{title} | {siteName}";

            // Maksimum uzunluk kontrolü
            if (withSite.Length <= MaxTitleLength)
            {
                return withSite;
            }

            // Site adı olmadan dene
            if (title.Length <= MaxTitleLength)
            {
                return title;
            }

            // Kırp
            return title.Substring(0, MaxTitleLength - 3) + "...";
        }

        public static string GenerateProductMetaDescription(
            string productName,
            string? summary = null,
            decimal? price = null,
            string? brandName = null)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return string.Empty;

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(summary))
            {
                // Summary varsa onu kullan
                parts.Add(TruncateText(summary, 100));
            }
            else
            {
                // Summary yoksa product adı + marka
                var intro = !string.IsNullOrWhiteSpace(brandName)
                    ? $"{brandName} {productName}"
                    : productName;
                parts.Add($"{intro} en uygun fiyatlarla satışta.");
            }

            // Fiyat bilgisi
            if (price.HasValue && price > 0)
            {
                parts.Add($"Fiyat: {price:N2} TL");
            }

            // CTA
            parts.Add("✓ Hızlı kargo ✓ Güvenli alışveriş");

            var description = string.Join(" ", parts);

            return TruncateDescription(description);
        }

        public static string GenerateCategoryMetaTitle(string categoryName, string siteName = "E-Ticaret")
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return siteName;

            var title = $"{categoryName} Ürünleri | {siteName}";

            if (title.Length <= MaxTitleLength)
            {
                return title;
            }

            // Kısa versiyon
            title = $"{categoryName} | {siteName}";

            if (title.Length <= MaxTitleLength)
            {
                return title;
            }

            // Sadece kategori adı
            return TruncateText(categoryName, MaxTitleLength);
        }

        public static string GenerateCategoryMetaDescription(
            string categoryName,
            string? description = null,
            int? productCount = null)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return string.Empty;

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(description))
            {
                // Açıklama varsa kullan
                parts.Add(TruncateText(description, 100));
            }
            else
            {
                // Açıklama yoksa otomatik oluştur
                parts.Add($"En iyi {categoryName} modelleri ve fiyatları.");
            }

            if (productCount.HasValue && productCount > 0)
            {
                parts.Add($"{productCount}+ ürün seçeneği.");
            }

            parts.Add("✓ Hızlı kargo ✓ Güvenli ödeme ✓ Kolay iade");

            var metaDescription = string.Join(" ", parts);

            return TruncateDescription(metaDescription);
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
                return text;

            // Son kelimeyi kesmeden kırp
            var truncated = text.Substring(0, maxLength - 3);
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > maxLength / 2)
            {
                truncated = truncated.Substring(0, lastSpace);
            }

            return truncated + "...";
        }

        private static string TruncateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            if (description.Length <= MaxDescriptionLength)
                return description;

            return TruncateText(description, MaxDescriptionLength);
        }
    }
}
