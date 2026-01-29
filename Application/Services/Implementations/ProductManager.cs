using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Security;
using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class ProductManager : IProductService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ProductManager> _logger;
        private readonly IHtmlSanitizerService _htmlSanitizer;

        public ProductManager(
            IRepositoryManager manager,
            IMapper mapper,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            ILogger<ProductManager> logger,
            IHtmlSanitizerService htmlSanitizer)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _logger = logger;
            _htmlSanitizer = htmlSanitizer;
        }

        public async Task<(IEnumerable<ProductDto> products, int count)> GetAllProductsAsync(ProductRequestParameters p)
        {
            var result = await _manager.Product.GetAllProductsAsync(p, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(result.products);

            return (productsDto, result.count);
        }

        public async Task<(IEnumerable<ProductDto> products, int count)> GetAllProductsAdminAsync(ProductRequestParametersAdmin p)
        {
            var result = await _manager.Product.GetAllProductsAdminAsync(p, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(result.products);

            return (productsDto, result.count);
        }

        public async Task<int> GetProductsCountAsync()
        {
            string cacheKey = "productsCount";

            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return cachedCount;
            }

            var count = await _manager.Product.GetProductsCountAsync();

            _cache.Set(cacheKey, count,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

            return count;
        }

        private async Task<Product> GetOneProductForServiceAsync(int id, bool trackChanges)
        {
            var product = await _manager.Product.GetOneProductAsync(id, trackChanges);
            if (product == null)
                throw new ProductNotFoundException(id);

            return product;
        }

        public async Task<ProductWithDetailsDto> GetOneProductAsync(int id)
        {
            var product = await GetOneProductForServiceAsync(id, false);
            var productDto = _mapper.Map<ProductWithDetailsDto>(product);

            return productDto;
        }

        public async Task<ProductWithDetailsDto> GetOneProductBySlugAsync(string slug)
        {
            var product = await _manager.Product.GetOneProductBySlugAsync(slug, false);
            if (product == null)
                throw new ProductNotFoundExceptionForSlug(slug);
            var productDto = _mapper.Map<ProductWithDetailsDto>(product);

            return productDto;
        }

        public async Task<IEnumerable<ProductDto>> GetRecommendedProductsAsync()
        {
            var products = await _manager.Product.GetRecommendedProductsAsync(false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetFavouriteProductsAsync(FavouriteResultDto favouritesDto)
        {
            var products = await _manager.Product.GetFavouriteProductsAsync(favouritesDto.FavouriteProductsId, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetLastestProductsAsync(int n)
        {
            var products = await _manager.Product.GetLastestProductsAsync(n, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetShowcaseProductsAsync()
        {
            var products = await _manager.Product.GetShowcaseProductsAsync(false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<OperationResult<int>> CreateProductAsync(ProductDtoForCreation productDto)
        {
            try
            {
                var product = _mapper.Map<Product>(productDto);

                if (!string.IsNullOrWhiteSpace(product.LongDescription))
                {
                    product.LongDescription = _htmlSanitizer.Sanitize(product.LongDescription);
                }

                product.Slug = SlugHelper.GenerateSlug(product.ProductName);

                var existingCount = await _manager.Product.CountBySlugAsync(product.Slug);
                if (existingCount > 0)
                {
                    product.Slug = SlugHelper.MakeUnique(product.Slug, existingCount + 1);
                }

                if (string.IsNullOrWhiteSpace(product.MetaTitle))
                {
                    product.MetaTitle = SeoMetaHelper.GenerateProductMetaTitle(product.ProductName, productDto.Brand);
                }

                if (string.IsNullOrWhiteSpace(product.MetaDescription))
                {
                    product.MetaDescription = SeoMetaHelper.GenerateProductMetaDescription(
                        product.ProductName,
                        productDto.Summary,
                        product.DiscountPrice ?? product.ActualPrice,
                        productDto.Brand);
                }

                product.ValidateForCreation();

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                _manager.Product.CreateProduct(product);
                product.CreatedByUserId = userId;
                product.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "Product",
                    entityId: product.ProductId.ToString(),
                    newValues: new
                    {
                        product.ProductName,
                        product.ActualPrice,
                        product.DiscountPrice,
                        product.Stock,
                        product.CategoryId
                    }
                );

                _logger.LogInformation(
                    "Product created successfully. ProductId: {ProductId}, Name: {ProductName}, User: {UserId}",
                    product.ProductId, product.ProductName, userId);

                return OperationResult<int>.Success(product.ProductId, "Ürün başarıyla oluşturuldu.");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning(ex, "Product validation failed. ProductName: {ProductName}", productDto.ProductName);
                return OperationResult<int>.Failure(ex.Message, ResultType.ValidationError);
            }
            catch (InvalidPriceException ex)
            {
                _logger.LogWarning(ex, "Invalid price. ActualPrice: {ActualPrice}, DiscountPrice: {DiscountPrice}",
                    productDto.ActualPrice, productDto.DiscountPrice);
                return OperationResult<int>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateProductImagesAsync(IEnumerable<ProductImageDtoForCreation> productImagesDto)
        {
            try
            {
                var product = await GetOneProductForServiceAsync(productImagesDto.First().ProductId, true);

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var productImage = _mapper.Map<IEnumerable<ProductImage>>(productImagesDto);

                foreach (var img in productImage)
                {
                    img.ValidateForCreation();

                    img.CreatedAt = DateTime.UtcNow;
                    img.CreatedByUserId = userId;
                    img.UpdatedAt = DateTime.UtcNow;
                    img.UpdatedByUserId = userId;

                    _manager.Product.CreateProductImage(img);
                    product.Images?.Add(img);
                }

                await _manager.SaveAsync();

                _logger.LogInformation(
                    "Product images updated. ProductId: {ProductId}, ImageCount: {Count}",
                    product.ProductId, productImagesDto.Count());

                return OperationResult<ProductWithDetailsDto>.Success("Ürün resimleri başarıyla güncellendi.");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning(ex, "Product image validation failed. ProductId: {ProductId}",
                    productImagesDto.First().ProductId);
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateProductShowcaseStatus(int id)
        {
            _manager.ClearTracker();
            var product = await GetOneProductForServiceAsync(id, true);
            var oldShowCase = product.ShowCase;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            product.ShowCase = !product.ShowCase;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Update",
                entityName: "Product",
                entityId: product.ProductId.ToString(),
                oldValues: new { ShowCase = oldShowCase },
                newValues: new { product.ShowCase }
            );

            _logger.LogInformation(
                "Product showcase status updated. ProductId: {ProductId}, ShowCase: {ShowCase}",
                product.ProductId, product.ShowCase);

            var productDto = _mapper.Map<ProductWithDetailsDto>(product);

            return OperationResult<ProductWithDetailsDto>.Success(
                productDto,
                product.ShowCase ? "Ürün vitrine eklendi." : "Ürün vitrinden kaldırıldı.");
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateProductAsync(ProductDtoForUpdate productDto)
        {
            try
            {
                _manager.ClearTracker();
                var product = await GetOneProductForServiceAsync(productDto.ProductId, true);

                var oldValues = new
                {
                    product.ProductName,
                    product.ActualPrice,
                    product.DiscountPrice,
                    product.Stock
                };

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                _mapper.Map(productDto, product);

                if (!string.IsNullOrWhiteSpace(product.LongDescription))
                {
                    product.LongDescription = _htmlSanitizer.Sanitize(product.LongDescription);
                }

                product.Slug = SlugHelper.GenerateSlug(product.ProductName);

                var existingCount = await _manager.Product.CountBySlugAsync(product.Slug);
                if (existingCount > 0)
                {
                    product.Slug = SlugHelper.MakeUnique(product.Slug, existingCount + 1);
                }

                if (string.IsNullOrWhiteSpace(product.MetaTitle))
                {
                    product.MetaTitle = SeoMetaHelper.GenerateProductMetaTitle(product.ProductName, productDto.Brand);
                }

                if (string.IsNullOrWhiteSpace(product.MetaDescription))
                {
                    product.MetaDescription = SeoMetaHelper.GenerateProductMetaDescription(
                        product.ProductName,
                        productDto.Summary,
                        product.DiscountPrice ?? product.ActualPrice,
                        productDto.Brand);
                }

                product.ValidatePrice();
                product.ValidateStock();

                product.UpdatedAt = DateTime.UtcNow;
                product.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "Product",
                    entityId: productDto.ProductId.ToString(),
                    oldValues: oldValues,
                    newValues: new
                    {
                        productDto.ProductName,
                        productDto.ActualPrice,
                        productDto.DiscountPrice,
                        productDto.Stock
                    }
                );

                _logger.LogInformation(
                    "Product updated successfully. ProductId: {ProductId}, User: {UserId}",
                    product.ProductId, userId);

                return OperationResult<ProductWithDetailsDto>.Success("Ürün başarıyla güncellendi.");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning(ex, "Product validation failed during update. ProductId: {ProductId}", productDto.ProductId);
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
            catch (InvalidPriceException ex)
            {
                _logger.LogWarning(ex, "Invalid price during update. ProductId: {ProductId}", productDto.ProductId);
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<ProductWithDetailsDto>> DeleteProductAsync(int id)
        {
            var product = await GetOneProductForServiceAsync(id, true);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            product.SoftDelete(userId);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "Product",
                entityId: id.ToString()
            );

            _logger.LogInformation(
                "Product soft deleted. ProductId: {ProductId}, User: {UserId}",
                id, userId);

            return OperationResult<ProductWithDetailsDto>.Success("Ürün başarıyla silindi.");
        }
    }
}
