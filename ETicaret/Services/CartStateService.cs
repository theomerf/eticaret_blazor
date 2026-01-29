using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Models;
using ETicaret.Extensions;
using System.Security.Claims;

namespace ETicaret.Services
{
    public interface ICartStateService
    {
        CartDto CurrentCart { get; }
        bool IsLoading { get; }
        string? ErrorMessage { get; }

        event Action? OnChange;

        Task LoadCartAsync(bool validate = true);
        Task SetQuantityAsync(int productId, int newQuantity);
        Task AddOrUpdateItemAsync(int productId, int quantity);
        Task RemoveItemAsync(int productId);
        Task ValidateCartAsync();
        
        IEnumerable<ProductDto> RecommendedProducts { get; }
        Task LoadRecommendedProductsAsync();
    }

    public class CartStateService : ICartStateService
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartStateService> _logger;
        private readonly Cart _sessionCart;

        public CartDto CurrentCart { get; private set; } = new();
        public bool IsLoading { get; private set; }
        public string? ErrorMessage { get; private set; }
        public IEnumerable<ProductDto> RecommendedProducts { get; private set; } = Enumerable.Empty<ProductDto>();

        public event Action? OnChange;

        public CartStateService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartStateService> logger,
            Cart sessionCart,
            ICartService cartService,
            IProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _sessionCart = sessionCart;
            _cartService = cartService;
            _productService = productService;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task LoadCartAsync(bool validate = true)
        {
            SetLoading(true);
            ClearError();

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;

                if (session != null)
                {
                    var sessionCart = session.GetJson<SessionCart>("cart");

                    if (sessionCart != null)
                    {
                        CurrentCart = new CartDto
                        {
                            UserId = "",
                            Lines = sessionCart.Lines.Select(l => new CartLineDto
                            {
                                ProductId = l.ProductId,
                                ProductName = l.ProductName,
                                ImageUrl = l.ImageUrl,
                                ActualPrice = l.ActualPrice,
                                DiscountPrice = l.DiscountPrice,
                                Quantity = l.Quantity
                            }).ToList()
                        };

                        if (_sessionCart is SessionCart sc)
                        {
                            sc.Lines.Clear();
                            sc.Lines.AddRange(sessionCart.Lines);
                            sc.UserId = sessionCart.UserId;
                            sc.Session = session;
                        }
                    }
                    else
                    {
                        CurrentCart = new CartDto
                        {
                            UserId = "",
                            Lines = new List<CartLineDto>()
                        };

                        if (_sessionCart is SessionCart sc)
                        {
                            sc.Lines.Clear();
                            sc.Session = session;
                        }
                    }
                }
                else
                {
                    CurrentCart = new CartDto
                    {
                        UserId = "",
                        Lines = new List<CartLineDto>()
                    };
                }

            }
            else
            {
                CurrentCart = await _cartService.GetCartAsync(userId, validate);

                await SyncSessionCartAsync();
            }

            SetLoading(false);
            NotifyStateChanged();
        }

        public async Task SetQuantityAsync(int productId, int newQuantity)
        {
            var userId = GetCurrentUserId();

            var line = CurrentCart.Lines.FirstOrDefault(l => l.ProductId == productId);
            if (line == null) return;

            var oldQuantity = line.Quantity;

            if (newQuantity == 0)
            {
                CurrentCart.Lines.Remove(line);
            }
            else
            {
                line.Quantity = newQuantity;
            }

            NotifyStateChanged();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null) return;

                var sessionCart = session.GetJson<SessionCart>("cart") ?? new SessionCart();
                sessionCart.Session = session;

                var product = sessionCart.Lines.FirstOrDefault(l => l.ProductId == productId);

                if (product != null)
                {
                    if (newQuantity == 0)
                    {
                        sessionCart.RemoveItem(productId);
                    }
                    else
                    {
                        sessionCart.SetQuantity(productId, newQuantity);
                    }

                    session.SetJson("cart", sessionCart);

                    if (session.IsAvailable)
                    {
                        await session.CommitAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Miktar güncellenirken session mevcut değil");
                    }
                    if (_sessionCart is SessionCart sc)
                    {
                        sc.Lines.Clear();
                        sc.Lines.AddRange(sessionCart.Lines);
                        sc.UserId = sessionCart.UserId;
                        sc.Session = session;
                    }

                    CurrentCart = new CartDto
                    {
                        UserId = "",
                        Lines = sessionCart.Lines.Select(l => new CartLineDto
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
            }
            else
            {
                var result = await _cartService.SetQuantityAsync(userId, productId, newQuantity);

                if (!result.IsSuccess)
                {
                    if (newQuantity == 0)
                    {
                        line.Quantity = oldQuantity;
                        CurrentCart.Lines.Add(line);
                    }
                    else
                    {
                        line.Quantity = oldQuantity;
                    }

                    SetError(result.Message);
                }
                else
                {
                    CurrentCart = await _cartService.GetCartAsync(userId);

                    await SyncSessionCartAsync();
                }
            }

            NotifyStateChanged();
        }

        public async Task AddOrUpdateItemAsync(int productId, int quantity)
        {
            var userId = GetCurrentUserId();

            SetLoading(true);
            ClearError();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    SetError("Session bulunamadı");
                    return;
                }

                var sessionCart = session.GetJson<SessionCart>("cart") ?? new SessionCart();
                sessionCart.Session = session;

                var product = await _productService.GetOneProductAsync(productId);

                if (product == null)
                {
                    SetError("Ürün bulunamadı");
                }
                else
                {
                    var productEntity = new Product
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        /*Images = product.Images, */
                        ActualPrice = product.ActualPrice,
                        DiscountPrice = product.DiscountPrice
                    };

                    var result = sessionCart.AddOrUpdateItem(productEntity, quantity);

                    if (result.IsSuccess)
                    {
                        session.SetJson("cart", sessionCart);

                        if (session.IsAvailable)
                        {
                            await session.CommitAsync();
                        }
                        else
                        {
                            _logger.LogWarning("Ürün eklenirken session mevcut değil");
                        }

                        if (_sessionCart is SessionCart sc)
                        {
                            sc.Lines.Clear();
                            sc.Lines.AddRange(sessionCart.Lines);
                            sc.UserId = sessionCart.UserId;
                            sc.Session = session;
                        }

                        CurrentCart = new CartDto
                        {
                            UserId = "",
                            Lines = sessionCart.Lines.Select(l => new CartLineDto
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
                    else
                    {
                        SetError(result.Message);
                    }
                }
            }
            else
            {
                var result = await _cartService.AddOrUpdateItemAsync(userId, productId, quantity);

                if (result.IsSuccess)
                {
                    CurrentCart = await _cartService.GetCartAsync(userId);

                    await SyncSessionCartAsync();
                }
                else
                {
                    SetError(result.Message);
                }
            }

            SetLoading(false);
            NotifyStateChanged();
        }

        public async Task RemoveItemAsync(int productId)
        {
            var userId = GetCurrentUserId();

            var line = CurrentCart.Lines.FirstOrDefault(l => l.ProductId == productId);
            if (line == null) return;

            CurrentCart.Lines.Remove(line);
            NotifyStateChanged();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null) return;

                var sessionCart = session.GetJson<SessionCart>("cart") ?? new SessionCart();
                sessionCart.Session = session;

                var result = sessionCart.RemoveItem(productId);

                if (result.IsSuccess)
                {
                    session.SetJson("cart", sessionCart);

                    if (session.IsAvailable)
                    {
                        await session.CommitAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Ürün kaldırılırken session mevcut değil");
                    }

                    if (_sessionCart is SessionCart sc)
                    {
                        sc.Lines.Clear();
                        sc.Lines.AddRange(sessionCart.Lines);
                        sc.UserId = sessionCart.UserId;
                        sc.Session = session;
                    }

                    CurrentCart = new CartDto
                    {
                        UserId = "",
                        Lines = sessionCart.Lines.Select(l => new CartLineDto
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
                else
                {
                    CurrentCart.Lines.Add(line);
                    SetError(result.Message);
                }
            }
            else
            {
                var result = await _cartService.RemoveItemAsync(userId, productId);

                if (result.IsSuccess)
                {
                    CurrentCart = await _cartService.GetCartAsync(userId);
                    await SyncSessionCartAsync();
                }
                else
                {
                    CurrentCart.Lines.Add(line);
                    SetError(result.Message);
                }
            }

            NotifyStateChanged();
        }

        public async Task LoadRecommendedProductsAsync()
        {
            RecommendedProducts = await _productService.GetRecommendedProductsAsync();
            NotifyStateChanged();
        }

        public async Task ValidateCartAsync()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var hasChanges = await _cartService.ValidateCartAsync(userId);

            if (hasChanges)
            {
                CurrentCart = await _cartService.GetCartAsync(userId);
                await SyncSessionCartAsync();

                SetError("Sepetinizde değişiklikler yapıldı (stok/fiyat)");
                NotifyStateChanged();
            }
        }

        private async Task SyncSessionCartAsync()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            _sessionCart.Lines.Clear();
            _sessionCart.UserId = CurrentCart.UserId;

            foreach (var lineDto in CurrentCart.Lines)
            {
                _sessionCart.Lines.Add(new CartLine
                {
                    ProductId = lineDto.ProductId,
                    ProductName = lineDto.ProductName,
                    ImageUrl = lineDto.ImageUrl,
                    ActualPrice = lineDto.ActualPrice,
                    DiscountPrice = lineDto.DiscountPrice,
                    Quantity = lineDto.Quantity,
                    Cart = _sessionCart
                });
            }

            if (_sessionCart is SessionCart sc)
            {
                sc.Session = session;
                session.SetJson("cart", sc);

                if (session.IsAvailable)
                {
                    await session.CommitAsync();
                }
                else
                {
                    _logger.LogWarning("SyncSessionCart sırasında session mevcut değil");
                }
            }
        }

        private void SetLoading(bool isLoading) => IsLoading = isLoading;
        private void SetError(string message) => ErrorMessage = message;
        private void ClearError() => ErrorMessage = null;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}