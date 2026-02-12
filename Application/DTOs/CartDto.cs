using Domain.Entities;

namespace Application.DTOs
{
    public class CartDto
    {
        public int CartId { get; set; }
        public string? UserId { get; set; }
        public List<CartLineDto> Lines { get; set; } = [];
        public int Version { get; set; }
        public bool IsUpdated { get; set; } = true;    

        public virtual void AddItem(Product product, int quantity)
        {
            CartLineDto? line = Lines.Where(l => l.ProductId.Equals(product.ProductId)).FirstOrDefault();

            if (line == null)
            {
                Lines.Add(new CartLineDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName ?? "",
                    ImageUrl = product.Variants?.FirstOrDefault()?.Images?.FirstOrDefault()?.ImageUrl,
                    ActualPrice = product.MinPrice,
                    DiscountPrice = null,
                    CartId = CartId,
                    Cart = this,
                    Quantity = quantity
                });
            }
            else
            {
                line.Quantity += quantity;
            }
        }

        public virtual void RemoveLine(int productId)
        {
            Lines.RemoveAll(l => l.ProductId.Equals(productId));
        }

        public virtual void IncreaseQuantity(int productId, int quantity)
        {
            CartLineDto? line = Lines.Where(l => l.ProductId.Equals(productId)).FirstOrDefault();

            if (line != null)
            {
                line.Quantity += quantity;
            }
        }

        public virtual void DecreaseQuantity(int productId, int quantity)
        {
            CartLineDto? line = Lines.Where(l => l.ProductId.Equals(productId)).FirstOrDefault();

            if (line != null)
            {
                line.Quantity -= quantity;
            }
        }

        public decimal ComputeTotalValue() =>
            Lines.Sum(e => e.ActualPrice * e.Quantity);

        public decimal ComputeTotalDiscountValue() =>
            Lines.Sum(e => e.DiscountPrice  ?? 0 * e.Quantity);

        public virtual void Clear() => Lines.Clear();
    }

}
