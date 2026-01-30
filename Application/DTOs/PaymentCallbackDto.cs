namespace Application.DTOs
{
    /// <summary>
    /// DTO for payment callback from external payment provider
    /// </summary>
    public record PaymentCallbackDto
    {
        public string OrderNumber { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public decimal? Amount { get; set; }
    }
}
