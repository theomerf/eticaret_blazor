namespace Application.DTOs
{
    public record PaymentCallbackDto
    {
        public string Token { get; set; } = null!; // Iyzico checkout form token
        public string OrderNumber { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public string? Provider { get; set; }
        public string? FailureReason { get; set; }
        public decimal? Amount { get; set; }
    }
}
