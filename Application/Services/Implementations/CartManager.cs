using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityLogService _securityLogService;

        public CartManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<CartManager> logger,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;
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

            return version ?? 0;
        }

        public async Task<CartOperationResult> SetQuantityAsync(string? userId, int productId, int productVariantId, int newQuantity)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Miktar güncellendi");
            }
            try
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
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "SetQuantityAsync concurrency hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepetiniz başka bir işlemle güncellendi. Lütfen tekrar deneyin.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "SetQuantityAsync veritabanı hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepet güncellenirken bir veritabanı hatası oluştu.");
            }
        }

        public async Task<CartOperationResult> AddOrUpdateItemAsync(string? userId, int productId, int productVariantId, int quantity)
        {
            ValidateUserAccess(userId);
            try
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
                var requestedTotal = (existingLine?.Quantity ?? 0) + quantity;
                if (requestedTotal > availableStock)
                {
                    return CartOperationResult.Failure($"Yetersiz stok. Mevcut: {availableStock}");
                }

                var cartResult = cart.AddOrUpdateItem(dbProduct, dbProductVariant, requestedTotal);
                if (!cartResult.IsSuccess)
                {
                    return cartResult;
                }

                await _manager.SaveAsync();
                _logger.LogInformation("Ürün eklendi. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);

                return CartOperationResult.Success("Ürün sepete eklendi", cart.Lines.Count, quantity);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "AddOrUpdateItemAsync concurrency hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepetiniz başka bir işlemle güncellendi. Lütfen tekrar deneyin.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "AddOrUpdateItemAsync veritabanı hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepete ürün eklenirken bir veritabanı hatası oluştu.");
            }
        }

        public async Task<CartOperationResult> RemoveItemAsync(string? userId, int productId, int productVariantId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Ürün sepetten kaldırıldı");
            }
            try
            {
                _manager.ClearTracker();
                var cart = await GetOrCreateCartAsync(userId, true);

                var result = cart.RemoveItem(productId, productVariantId);
                if (!result.IsSuccess)
                {
                    return result;
                }

                await _manager.SaveAsync();
                _logger.LogInformation("Ürün kaldırıldı. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);

                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "RemoveItemAsync concurrency hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepetiniz başka bir işlemle güncellendi. Lütfen tekrar deneyin.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "RemoveItemAsync veritabanı hatası. Kullanıcı: {UserId}, Ürün: {ProductId}, Varyant: {VariantId}", userId, productId, productVariantId);
                return CartOperationResult.Failure("Sepetten ürün kaldırılırken bir veritabanı hatası oluştu.");
            }
        }

        public async Task<CartOperationResult> ClearAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId))
            {
                return CartOperationResult.Success("Sepet temizlendi");
            }
            try
            {
                _manager.ClearTracker();
                var cart = await GetOrCreateCartAsync(userId, true);
                cart.Clear();

                await _manager.SaveAsync();
                _logger.LogInformation("Sepet temizlendi. Kullanıcı: {UserId}", userId);

                return CartOperationResult.Success("Sepet temizlendi");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "ClearAsync concurrency hatası. Kullanıcı: {UserId}", userId);
                return CartOperationResult.Failure("Sepetiniz başka bir işlemle güncellendi. Lütfen tekrar deneyin.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "ClearAsync veritabanı hatası. Kullanıcı: {UserId}", userId);
                return CartOperationResult.Failure("Sepet temizlenirken bir veritabanı hatası oluştu.");
            }
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
            try
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
                    var dbProduct = await _manager.Product.GetByIdAsync(sessionLine.ProductId, false, false);
                    var dbVariant = await _manager.ProductVariant.GetByIdAsync(sessionLine.ProductVariantId, false, false);

                    if (dbProduct == null || dbVariant == null || dbVariant.ProductId != sessionLine.ProductId)
                    {
                        continue;
                    }

                    var dbLine = cart.Lines.FirstOrDefault(l => l.ProductId == sessionLine.ProductId && l.ProductVariantId == sessionLine.ProductVariantId);
                    var requestedTotal = (dbLine?.Quantity ?? 0) + sessionLine.Quantity;
                    if (requestedTotal <= 0)
                    {
                        continue;
                    }

                    if (requestedTotal > dbVariant.Stock)
                    {
                        requestedTotal = dbVariant.Stock;
                    }

                    var result = cart.AddOrUpdateItem(dbProduct, dbVariant, requestedTotal);
                    if (result.IsSuccess)
                    {
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
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "MergeCartsAsync concurrency hatası. Kullanıcı: {UserId}", userId);
                return await GetByUserIdAsync(userId, false);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "MergeCartsAsync veritabanı hatası. Kullanıcı: {UserId}", userId);
                return await GetByUserIdAsync(userId, false);
            }
        }

        public async Task<bool> ValidateAsync(string? userId)
        {
            ValidateUserAccess(userId);

            if (string.IsNullOrEmpty(userId)) return false;
            try
            {
                _manager.ClearTracker();
                var cart = await _manager.Cart.GetByUserIdAsync(userId, true);
                if (cart == null) return false;

                return await ValidateCartItemsAsync(cart);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "ValidateAsync concurrency hatası. Kullanıcı: {UserId}", userId);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "ValidateAsync veritabanı hatası. Kullanıcı: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ValidateCartItemsAsync(Cart cart)
        {
            bool hasChanges = false;
            var itemsToRemove = new List<CartLine>();
            bool requiresVersionTouch = false;

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
                    requiresVersionTouch = true;
                    if (line.Quantity == 0) itemsToRemove.Add(line);
                }

                if (line.Price != variant.Price || line.DiscountPrice != variant.DiscountPrice)
                {
                    line.Price = variant.Price;
                    line.DiscountPrice = variant.DiscountPrice;
                    hasChanges = true;
                    requiresVersionTouch = true;
                }

                if (line.SpecificationsJson != variant.VariantSpecificationsJson)
                {
                    line.SpecificationsJson = variant.VariantSpecificationsJson;
                    hasChanges = true;
                    requiresVersionTouch = true;
                }
            }

            foreach (var item in itemsToRemove)
            {
                cart.Lines.Remove(item);
                requiresVersionTouch = true;
            }

            if (hasChanges)
            {
                if (requiresVersionTouch)
                {
                    cart.MarkUpdated();
                }

                await _manager.SaveAsync();
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
