namespace Application.DTOs
{
    public record DailySalesDto
    {
        public DateTime Date { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal? TotalRevenue { get; set; }
    }
}
