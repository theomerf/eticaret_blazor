using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Application.Services.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class CartManager : ICartService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ILogger<CartManager> _logger;
        private readonly ResiliencePipeline _retryPipeline;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityLogService _securityLogService;

        public CartManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<CartManager> logger,
            Cart sessionCart,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;

            _retryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(100),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>()
                        .Handle<DbUpdateException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning("{Exception} hatası nedeniyle işlem {Duration}ms sonra {RetryCount}. kez tekrar ediliyor.", args.Outcome.Exception?.Message, args.RetryDelay.TotalMilliseconds, args.AttemptNumber);

                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private void ValidateUserAccess(string? requestedUserId)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            if (requestedUserId != currentUserId)
            {
                _securityLogService.LogUnauthorizedAccessAsync(
                    userId: currentUserId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }
        }

        public async Task<int> GetCartLinesCountAsync(string userId)
        {
            ValidateUserAccess(userId);
            return await _manager.Cart.GetCartLinesCountAsync(userId);
        }

        public async Task<int> GetCartVersionAsync(string userId)
        {
            var version = await _manager.Cart.GetCartVersionAsync(userId);

            if (version == 0 || version == null)
            {
                throw new KeyNotFoundException("Sepet bulunamadı");
            }

            return version.Value;
        }

        public async Task<CartOperationResult> SetQuantityAsync(string? userId, int productId, int newQuantity)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Miktar güncellendi");
            }

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();
                var cart = await GetOrCreateCartAsync(userId, true);

                if (newQuantity > 0)
                {
                    var product = await _manager.Product.GetOneProductAsync(productId, false);

                    if (product == null)
                    {
                        return CartOperationResult.Failure("Ürün bulunamadı");
                    }

                    if (product.Stock < newQuantity)
                    {
                        return CartOperationResult.Failure($"Yetersiz stok. Mevcut: {product.Stock}");
                    }

                    var line = cart.Lines.FirstOrDefault(l => l.ProductId == productId);
                    if (line != null)
                    {
                        line.ActualPrice = product.ActualPrice;
                        line.DiscountPrice = product.DiscountPrice;
                    }
                }

                var result = cart.SetQuantity(productId, newQuantity);

                if (result.IsSuccess)
                {
                    await _manager.SaveAsync();
                    _logger.LogInformation("Miktar ayarlandı. Kullanıcı: {UserId}, Ürün: {ProductId}, Miktar: {Quantity}",
                        userId, productId, newQuantity);
                }

                return result;
            }, CancellationToken.None);
        }

        public async Task<CartOperationResult> AddOrUpdateItemAsync(string? userId, int productId, int quantity)
        {
            ValidateUserAccess(userId);

            var dbProduct = await _manager.Product.GetOneProductAsync(productId, false);

            if (dbProduct == null)
            {
                return CartOperationResult.Failure("Ürün bulunamadı");
            }

            if (dbProduct.Stock < quantity)
            {
                return CartOperationResult.Failure($"Yetersiz stok. Mevcut stok: {dbProduct.Stock}");
            }

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Ürün sepete eklendi", 0, quantity);
            }

            _manager.ClearTracker();
            var cart = await GetOrCreateCartAsync(userId, true);
            var result = cart.AddOrUpdateItem(dbProduct, quantity);

            if (result.IsSuccess)
            {
                await _manager.SaveAsync();
                _logger.LogInformation("Ürün eklendi. Kullanıcı: {UserId}, Ürün: {ProductId}", userId, productId);
            }

            return result;
        }

        public async Task<CartOperationResult> RemoveItemAsync(string? userId, int productId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Ürün sepetten kaldırıldı");
            }

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();
                var cart = await GetOrCreateCartAsync(userId, true);
                var result = cart.RemoveItem(productId);

                if (result.IsSuccess)
                {
                    await _manager.SaveAsync();
                    _logger.LogInformation("Ürün kaldırıldı. Kullanıcı: {UserId}, Ürün: {ProductId}", userId, productId);
                }

                return result;
            }, CancellationToken.None);
        }

        public async Task<CartOperationResult> ClearCartAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Sepet temizlendi");
            }

            _manager.ClearTracker();
            var cart = await GetOrCreateCartAsync(userId, true);
            cart.Clear();

            await _manager.SaveAsync();
            _logger.LogInformation("Sepet temizlendi. Kullanıcı: {UserId}", userId);

            return CartOperationResult.Success("Sepet temizlendi");
        }

        public async Task<CartDto> GetCartAsync(string? userId, bool validate = false)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return new CartDto
                {
                    UserId = "",
                    Lines = new List<CartLineDto>()
                };
            }

            var cart = await _manager.Cart.GetCartByUserIdAsync(userId, false);

            if (cart == null)
            {
                return new CartDto
                {
                    UserId = userId,
                    Lines = new List<CartLineDto>()
                };
            }

            if (validate)
            {
                _manager.ClearTracker();
                cart = await _manager.Cart.GetCartByUserIdAsync(userId, true);
                await ValidateCartItemsAsync(cart!);
            }

            var cartDto = _mapper.Map<CartDto>(cart);

            return cartDto;
        }

        public async Task<CartDto> MergeCartsAsync(string userId, CartDto sessionCart)
        {
            ValidateUserAccess(userId);

            _manager.ClearTracker();

            if (!sessionCart.Lines.Any())
            {
                return await GetCartAsync(userId, false);
            }

            var cart = await GetOrCreateCartAsync(userId, true);
            bool hasChanges = false;

            foreach (var sessionLine in sessionCart.Lines)
            {
                var dbLine = cart.Lines.FirstOrDefault(l => l.ProductId == sessionLine.ProductId);

                if (dbLine != null)
                {
                    dbLine.Quantity += sessionLine.Quantity;
                    hasChanges = true;
                }
                else
                {
                    cart.Lines.Add(new CartLine
                    {
                        ProductId = sessionLine.ProductId,
                        ProductName = sessionLine.ProductName,
                        ImageUrl = sessionLine.ImageUrl,
                        ActualPrice = sessionLine.ActualPrice,
                        DiscountPrice = sessionLine.DiscountPrice,
                        Quantity = sessionLine.Quantity
                    });
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _manager.SaveAsync();
                _logger.LogInformation("Sepetler birleştirildi. Kullanıcı: {UserId}. Toplam ürün sayısı: {Count}",
                    userId, cart.Lines.Count);
            }

            return _mapper.Map<CartDto>(cart);
        }

        public async Task<bool> ValidateCartAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId)) return false;

            var cart = await _manager.Cart.GetCartByUserIdAsync(userId, true);
            if (cart == null) return true;

            return await ValidateCartItemsAsync(cart);
        }

        public async Task<bool> ValidateCartItemsAsync(Cart cart)
        {
            bool hasChanges = false;
            var itemsToRemove = new List<CartLine>();

            foreach (var line in cart.Lines)
            {
                var product = await _manager.Product.GetOneProductAsync(line.ProductId, false);

                if (product == null)
                {
                    itemsToRemove.Add(line);
                    hasChanges = true;
                    continue;
                }

                if (product.Stock < line.Quantity)
                {
                    line.Quantity = product.Stock;
                    hasChanges = true;

                    if (line.Quantity == 0)
                    {
                        itemsToRemove.Add(line);
                    }
                }

                if (line.ActualPrice != product.ActualPrice || line.DiscountPrice != product.DiscountPrice)
                {
                    line.ActualPrice = product.ActualPrice;
                    line.DiscountPrice = product.DiscountPrice;
                    hasChanges = true;
                }
            }

            foreach (var item in itemsToRemove)
            {
                cart.Lines.Remove(item);
            }

            if (hasChanges)
            {
                await _manager.SaveAsync();

                var cartDto = _mapper.Map<CartDto>(cart);
            }

            return hasChanges;
        }

        private async Task<Cart> GetOrCreateCartAsync(string userId, bool trackChanges)
        {
            var cart = await _manager.Cart.GetCartByUserIdAsync(userId, trackChanges);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                };

                _manager.Cart.CreateCart(cart);
                await _manager.SaveAsync();
            }

            return cart;
        }
    }
}
