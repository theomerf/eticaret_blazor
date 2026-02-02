using Application.DTOs;

namespace Application.Common.Models
{
    public class IyzicoCheckoutFormInitRequest
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal SubTotal { get; set; }
        public string Currency { get; set; } = "TRY";
        public string CustomerEmail { get; set; } = null!;
        public string CustomerIdentityNumber { get; set; } = null!;
        public AddressDto BillingAddress { get; set; } = null!;
        public ICollection<OrderLineDto> OrderLines { get; set; } = new List<OrderLineDto>();
        public string CallbackUrl { get; set; } = null!;
    }
}
