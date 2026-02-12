namespace ETicaret.Helpers
{
    public static class ColorHelper
    {
        public static string GetColorHex(string? colorName)
        {
            return colorName?
                .Trim()
                .ToLowerInvariant() switch
            {
                "siyah" => "#000000",
                "beyaz" => "#ffffff",
                "kırmızı" => "#ff0000",
                "mavi" => "#0000ff",
                "yeşil" => "#008000",
                "sarı" => "#ffff00",
                "gri" => "#808080",
                "lacivert" => "#000080",
                "bej" => "#f5f5dc",
                "turuncu" => "#ffa500",
                "mor" => "#800080",
                "pembe" => "#ffc0cb",
                "kahverengi" => "#a52a2a",
                _ => "#d1d5db"
            };
        }
    }
}
