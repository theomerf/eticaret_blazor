using Domain.Entities;

namespace Application.Queries.RequestParameters
{
    public record OrderRequestParametersAdmin : RequestParametersAdmin
    {
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
    }
}
