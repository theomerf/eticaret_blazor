using Application.Common.Models;
using Application.DTOs;

namespace ETicaret.Models
{
    public class PaymentViewModel
    {
        public OrderWithDetailsDto Order { get; set; } = null!;
        public string? PaymentUrl { get; set; }
        public string TransactionId { get; set; } = null!;
    }
}
