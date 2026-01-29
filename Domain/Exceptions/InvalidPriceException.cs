namespace Domain.Exceptions
{
    public class InvalidPriceException : Exception
    {
        public decimal ActualPrice { get; }
        public decimal? DiscountPrice { get; }

        public InvalidPriceException(decimal actualPrice, decimal? discountPrice)
            : base($"Geçersiz fiyat. İndirimli fiyat ({discountPrice}) normal fiyattan ({actualPrice}) büyük olamaz.")
        {
            ActualPrice = actualPrice;
            DiscountPrice = discountPrice;
        }

        public InvalidPriceException(string message) : base(message)
        {
        }
    }
}
