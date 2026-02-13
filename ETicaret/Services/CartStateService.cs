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
        Task SetQuantityAsync(int productId, int variantId, int newQuantity);
        Task AddOrUpdateItemAsync(int productId, int variantId, int quantity);
        Task RemoveItemAsync(int productId, int variantId);
        Task ClearCartAsync();
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
                                Price = l.Price,
                                DiscountPrice = l.DiscountPrice,
                                Quantity = l.Quantity,
                                ProductVariantId = l.ProductVariantId,
                                SelectedColor = l.SelectedColor,
                                SelectedSize = l.SelectedSize
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
                CurrentCart = await _cartService.GetByUserIdAsync(userId, validate);

                await SyncSessionCartAsync();
            }

            SetLoading(false);
            NotifyStateChanged();
        }

        public async Task SetQuantityAsync(int productId, int variantId, int newQuantity)
        {
            var userId = GetCurrentUserId();

            var line = CurrentCart.Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == variantId);
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
                        sessionCart.RemoveItem(productId, variantId);
                    }
                    else
                    {
                        sessionCart.SetQuantity(productId, variantId, newQuantity);
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
                            Price = l.Price,
                            DiscountPrice = l.DiscountPrice,
                            Quantity = l.Quantity,
                            ProductVariantId = l.ProductVariantId,
                            SelectedColor = l.SelectedColor,
                            SelectedSize = l.SelectedSize
                        }).ToList()
                    };
                }
            }
            else
            {
                var result = await _cartService.SetQuantityAsync(userId, productId, variantId, newQuantity);

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
                    CurrentCart = await _cartService.GetByUserIdAsync(userId);

                    await SyncSessionCartAsync();
                }
            }

            NotifyStateChanged();
        }

        public async Task AddOrUpdateItemAsync(int productId, int variantId, int quantity)
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

                var product = await _productService.GetByIdAsync(productId);
                
                if (product == null)
                {
                    SetError("Ürün bulunamadı");
                    return;
                }

                if (product.Variants.FirstOrDefault(v => v.ProductVariantId == variantId) == null)
                {
                    SetError("Geçersiz ürün varyantı");
                }

                var variantDto = await _productService.GetVariantByIdAsync(variantId, true);

                if (variantDto == null)
                {
                    SetError("Varyant bulunamadı");
                    return;
                }

                // Map DTO to Entity manually since we don't have automapper here and types mismatch
                ProductVariant? variantEntity = null;
                if (variantDto != null)
                {
                    variantEntity = new ProductVariant
                    {
                        ProductVariantId = variantDto.ProductVariantId,
                        ProductId = product!.ProductId,
                        Price = variantDto.Price,
                        DiscountPrice = variantDto.DiscountPrice,
                        Stock = variantDto.Stock,
                        Color = variantDto.Color,
                        Size = variantDto.Size,
                        Images = variantDto.Images?.Where(i => i.IsPrimary == true).Select(i => new ProductImage
                        {
                            ProductImageId = i.ProductImageId,
                            ProductVariantId = i.ProductVariantId,
                            ImageUrl = i.ImageUrl,
                            IsPrimary = i.IsPrimary,
                        }).ToList()
                    };
                }

                var productEntity = new Product
                {
                        ProductId = product!.ProductId,
                    ProductName = product.ProductName
                };

                // For simple products (no variants), we might have variantId=null. 
                // But in new system, every product must have at least one variant.
                // If variantEntity is still null here, it means something is wrong with product data or request.
                if (variantEntity == null)
                {
                    SetError("Ürün varyant verisi eksik");
                    return; 
                }

                var result = sessionCart.AddOrUpdateItem(productEntity, variantEntity, quantity);

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
                            Price = l.Price,
                            DiscountPrice = l.DiscountPrice,
                            Quantity = l.Quantity,
                            ProductVariantId = l.ProductVariantId,
                            SelectedColor = l.SelectedColor,
                            SelectedSize = l.SelectedSize
                        }).ToList()
                    };
                }
                else
                {
                    SetError(result.Message);
                }

            }
            else
            {
                var result = await _cartService.AddOrUpdateItemAsync(userId, productId, variantId, quantity);

                if (result.IsSuccess)
                {
                    CurrentCart = await _cartService.GetByUserIdAsync(userId);

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

        public async Task RemoveItemAsync(int productId, int variantId)
        {
            var userId = GetCurrentUserId();

            var line = CurrentCart.Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == variantId);
            if (line == null) return;

            CurrentCart.Lines.Remove(line);
            NotifyStateChanged();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null) return;

                var sessionCart = session.GetJson<SessionCart>("cart") ?? new SessionCart();
                sessionCart.Session = session;

                var result = sessionCart.RemoveItem(productId, variantId);

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
                            Price = l.Price,
                            DiscountPrice = l.DiscountPrice,
                            Quantity = l.Quantity,
                            ProductVariantId = l.ProductVariantId,
                            SelectedColor = l.SelectedColor,
                            SelectedSize = l.SelectedSize
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
                var result = await _cartService.RemoveItemAsync(userId, productId, variantId);

                if (result.IsSuccess)
                {
                    CurrentCart = await _cartService.GetByUserIdAsync(userId);
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

        public async Task ClearCartAsync()
        {
            var userId = GetCurrentUserId();

            CurrentCart.Lines.Clear();
            NotifyStateChanged();

            if (string.IsNullOrEmpty(userId))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null) return;

                var sessionCart = session.GetJson<SessionCart>("cart") ?? new SessionCart();
                sessionCart.Session = session;
                
                sessionCart.Lines.Clear();
                session.SetJson("cart", sessionCart);

                if (session.IsAvailable)
                {
                    await session.CommitAsync();
                }

                if (_sessionCart is SessionCart sc)
                {
                    sc.Lines.Clear();
                    sc.UserId = sessionCart.UserId;
                    sc.Session = session;
                }
            }
            else
            {
                var result = await _cartService.ClearAsync(userId); // Use ClearAsync method from _cartService

                if (!result.IsSuccess)
                {
                    SetError(result.Message);
                    // Revert state if failed
                    CurrentCart = await _cartService.GetByUserIdAsync(userId);
                    await SyncSessionCartAsync();
                    NotifyStateChanged();
                }
            }
        }

        public async Task LoadRecommendedProductsAsync()
        {
            RecommendedProducts = await _productService.GetRecommendationsAsync();
            NotifyStateChanged();
        }

        public async Task ValidateCartAsync()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var hasChanges = await _cartService.ValidateAsync(userId);

            if (hasChanges)
            {
                CurrentCart = await _cartService.GetByUserIdAsync(userId);
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
                    Price = lineDto.Price,
                    DiscountPrice = lineDto.DiscountPrice,
                    Quantity = lineDto.Quantity,
                    ProductVariantId = lineDto.ProductVariantId,
                    SelectedColor = lineDto.SelectedColor,
                    SelectedSize = lineDto.SelectedSize,
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