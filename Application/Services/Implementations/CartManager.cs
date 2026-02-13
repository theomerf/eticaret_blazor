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
using System.Text.Json;

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

        public async Task<int> CountOfLinesAsync(string userId)
        {
            ValidateUserAccess(userId);
            return await _manager.Cart.CountOfLinesAsync(userId);
        }

        public async Task<int> GetVersionAsync(string userId)
        {
            var version = await _manager.Cart.GetVersionAsync(userId);

            if (version == 0 || version == null)
            {
                throw new KeyNotFoundException("Sepet bulunamadı");
            }

            return version.Value;
        }

        public async Task<CartOperationResult> SetQuantityAsync(string? userId, int productId, int productVariantId, int newQuantity)
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
                    var productVariant = await _manager.ProductVariant.GetByIdAsync(productVariantId, false, false);

                    if (productVariant == null)
                    {
                        return CartOperationResult.Failure("Ürün bulunamadı");
                    }

                    int availableStock = productVariant.Stock;

                    if (availableStock < newQuantity)
                    {
                        return CartOperationResult.Failure($"Yetersiz stok. Mevcut: {availableStock}");
                    }

                    var line = cart.Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == productVariantId);
                    if (line != null)
                    {
                        line.Price = productVariant.Price;
                        line.DiscountPrice = productVariant.DiscountPrice;
                    }
                }

                var result = cart.SetQuantity(productId, productVariantId, newQuantity);

                if (result.IsSuccess)
                {
                    await _manager.SaveAsync();
                    _logger.LogInformation("Miktar ayarlandı. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}, Miktar: {Quantity}",
                        userId, productId, productVariantId, newQuantity);
                }

                return result;
            }, CancellationToken.None);
        }

        public async Task<CartOperationResult> AddOrUpdateItemAsync(string? userId, int productId, int productVariantId, int quantity)
        {
            ValidateUserAccess(userId);

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();
                var dbProduct = await _manager.Product.GetByIdAsync(productId, false, false);
                var dbProductVariant = await _manager.ProductVariant.GetByIdAsync(productVariantId, true, false);

                if (dbProduct == null)
                {
                    return CartOperationResult.Failure("Ürün bulunamadı");
                }

                if (dbProductVariant == null)
                {
                    return CartOperationResult.Failure("Ürün bulunamadı");
                }

                if (dbProductVariant.ProductId != productId)
                {
                    return CartOperationResult.Failure("Varyant, belirtilen ürüne ait değil");
                }

                var availableStock = dbProductVariant.Stock;

                if (availableStock < quantity)
                {
                    return CartOperationResult.Failure($"Yetersiz stok. Mevcut stok: {availableStock}");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return CartOperationResult.Success("Ürün sepete eklendi", 0, quantity);
                }

                var cart = await GetOrCreateCartAsync(userId, true);

                var existingLine = cart.Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == productVariantId);

                if (existingLine != null)
                {
                    existingLine.Quantity += quantity;
                    existingLine.Price = dbProductVariant.Price;
                    existingLine.DiscountPrice = dbProductVariant.DiscountPrice;
                }
                else
                {
                    cart.Lines.Add(new CartLine
                    {
                        ProductId = productId,
                        ProductName = dbProduct.ProductName,
                        ImageUrl = dbProductVariant.Images?.FirstOrDefault()?.ImageUrl,
                        Price = dbProductVariant.Price,
                        DiscountPrice = dbProductVariant.DiscountPrice,
                        Quantity = quantity,
                        ProductVariantId = productVariantId,
                        SelectedColor = dbProductVariant.Color,
                        SelectedSize = dbProductVariant.Size,
                        SpecificationsJson = dbProductVariant.VariantSpecificationsJson
                    });
                }

                await _manager.SaveAsync();
                _logger.LogInformation("Ürün eklendi. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);

                return CartOperationResult.Success("Ürün sepete eklendi", cart.Lines.Count, quantity);
            }, CancellationToken.None);
        }

        public async Task<CartOperationResult> RemoveItemAsync(string? userId, int productId, int productVariantId)
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

                var lineToRemove = cart.Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == productVariantId);

                if (lineToRemove == null)
                {
                    return CartOperationResult.Failure("Ürün sepette bulunamadı");
                }

                cart.Lines.Remove(lineToRemove);
                await _manager.SaveAsync();
                _logger.LogInformation("Ürün kaldırıldı. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);

                return CartOperationResult.Success("Ürün sepetten kaldırıldı");
            }, CancellationToken.None);
        }

        public async Task<CartOperationResult> ClearAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Sepet temizlendi");
            }

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();
                var cart = await GetOrCreateCartAsync(userId, true);
                cart.Clear();

                await _manager.SaveAsync();
                _logger.LogInformation("Sepet temizlendi. Kullanıcı: {UserId}", userId);

                return CartOperationResult.Success("Sepet temizlendi");
            }, CancellationToken.None);
        }

        public async Task<CartDto> GetByUserIdAsync(string? userId, bool validate = false)
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

            var cart = await _manager.Cart.GetByUserIdAsync(userId, false);

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
                await ValidateCartItemsAsync(cart);
            }

            var cartDto = _mapper.Map<CartDto>(cart);

            return cartDto;
        }

        public async Task<CartDto> MergeCartsAsync(string userId, CartDto sessionCart)
        {
            ValidateUserAccess(userId);

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();

                if (!sessionCart.Lines.Any())
                {
                    return await GetByUserIdAsync(userId, false);
                }

                var cart = await GetOrCreateCartAsync(userId, true);
                bool hasChanges = false;

                foreach (var sessionLine in sessionCart.Lines)
                {
                    var dbLine = cart.Lines.FirstOrDefault(l => l.ProductId == sessionLine.ProductId && l.ProductVariantId == sessionLine.ProductVariantId);

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
                            Price = sessionLine.Price,
                            DiscountPrice = sessionLine.DiscountPrice,
                            Quantity = sessionLine.Quantity,
                            ProductVariantId = sessionLine.ProductVariantId,
                            SelectedColor = sessionLine.SelectedColor,
                            SelectedSize = sessionLine.SelectedSize,
                            SpecificationsJson = JsonSerializer.Serialize(sessionLine.VariantSpecifications.Select(x => new Application.DTOs.ProductSpecificationDto { Key = x.Key, Value = x.Value }), (JsonSerializerOptions?)null)
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
            }, CancellationToken.None);
        }

        public async Task<bool> ValidateAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId)) return false;

            return await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                _manager.ClearTracker();
                var cart = await _manager.Cart.GetByUserIdAsync(userId, true);
                if (cart == null) return true;

                return await ValidateCartItemsAsync(cart);
            }, CancellationToken.None);
        }

        public async Task<bool> ValidateCartItemsAsync(Cart cart)
        {
            bool hasChanges = false;
            var itemsToRemove = new List<CartLine>();

            foreach (var line in cart.Lines)
            {
                var product = await _manager.Product.GetByIdAsync(line.ProductId, false, false);

                if (product == null)
                {
                    itemsToRemove.Add(line);
                    hasChanges = true;
                    continue;
                }

                var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, false);

                if (variant == null)
                {
                    itemsToRemove.Add(line);
                    hasChanges = true;
                    continue;
                }

                if (variant.Stock < line.Quantity)
                {
                    line.Quantity = variant.Stock;
                    hasChanges = true;
                    if (line.Quantity == 0) itemsToRemove.Add(line);
                }

                if (line.Price != variant.Price || line.DiscountPrice != variant.DiscountPrice)
                {
                    line.Price = variant.Price;
                    line.DiscountPrice = variant.DiscountPrice;
                    hasChanges = true;
                }

                if (line.SpecificationsJson != variant.VariantSpecificationsJson)
                {
                    line.SpecificationsJson = variant.VariantSpecificationsJson;
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
            var cart = await _manager.Cart.GetByUserIdAsync(userId, trackChanges);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                };

                _manager.Cart.Create(cart);
                await _manager.SaveAsync();
            }

            return cart;
        }
    }
}
