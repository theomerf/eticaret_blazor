using Application.DTOs;
using Domain.Entities;
using ETicaret.Extensions;
using System.Text.Json.Serialization;

namespace ETicaret.Models
{
    public class SessionCart : Cart
    {
        [JsonIgnore]
        public ISession? Session { get; set; }

        public static Cart GetCart(IServiceProvider services)
        {
            ISession? session = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Session;

            SessionCart cart = session?.GetJson<SessionCart>("cart") ?? new SessionCart();
            cart.Session = session;
            return cart;
        }

        public static CartDto GetCartDto(ISession? session)
        {
            if (session == null)
            {
                return new CartDto
                {
                    UserId = "",
                    Lines = new List<CartLineDto>()
                };
            }

            SessionCart cart = session.GetJson<SessionCart>("cart") ?? new SessionCart();

            return new CartDto()
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                Lines = cart.Lines.Select(l => new CartLineDto
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    ImageUrl = l.ImageUrl,
                    ActualPrice = l.ActualPrice,
                    DiscountPrice = l.DiscountPrice,
                    Quantity = l.Quantity
                }).ToList()
            };
        }

        public override CartOperationResult SetQuantity(int prouductId, int newQuantity)
        {
            var result = base.SetQuantity(prouductId, newQuantity);

            if (result.IsSuccess)
            {
                SaveToSession();
            }

            return result;
        }

        public override CartOperationResult AddOrUpdateItem(Product product, int quantity)
        {
            var result = base.AddOrUpdateItem(product, quantity);

            if (result.IsSuccess)
            {
                SaveToSession();
            }

            return result;
        }

        public override CartOperationResult RemoveItem(int productId)
        {
            var result = base.RemoveItem(productId);

            if (result.IsSuccess)
            {
                SaveToSession();
            }

            return result;
        }

        public override void Clear()
        {
            base.Clear();
            Session?.Remove("cart");
        }

        private void SaveToSession()
        {
            if (Session != null)
            {
                Session.SetJson("cart", this);
            }
        }

        public void UpdateFromDto(CartDto cartDto, ISession? session)
        {
            Session = session;
            UserId = cartDto.UserId;
            CartId = cartDto.CartId;

            Lines.Clear();

            foreach (var lineDto in cartDto.Lines)
            {
                Lines.Add(new CartLine
                {
                    ProductId = lineDto.ProductId,
                    ProductName = lineDto.ProductName,
                    ImageUrl = lineDto.ImageUrl,
                    ActualPrice = lineDto.ActualPrice,
                    DiscountPrice = lineDto.DiscountPrice,
                    Quantity = lineDto.Quantity,
                    Cart = this
                });
            }

            SaveToSession();
        }
    }
}
