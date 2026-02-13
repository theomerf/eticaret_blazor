using Domain.Entities;

namespace Application.DTOs
{
    public class CartDto
    {
        public int CartId { get; set; }
        public string? UserId { get; set; }
        public List<CartLineDto> Lines { get; set; } = [];
        public int Version { get; init; }
        public bool IsUpdated { get; set; } = true;    

        public decimal ComputeTotalValue() =>
            Lines.Sum(e => e.Price * e.Quantity);

        public decimal ComputeTotalDiscountValue() =>
            Lines.Sum(e => e.DiscountPrice  ?? 0 * e.Quantity);

        public virtual void Clear() => Lines.Clear();
    }

}
