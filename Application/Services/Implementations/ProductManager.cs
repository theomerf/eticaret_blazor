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
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Application.Services.Implementations
{
    public class ProductManager : IProductService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly IActivityService _activityService;
        private readonly ILogger<ProductManager> _logger;
        private readonly IHtmlSanitizerService _htmlSanitizer;

        public ProductManager(
            IRepositoryManager manager,
            IMapper mapper,
            ICacheService cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            IActivityService activityService,
            ILogger<ProductManager> logger,
            IHtmlSanitizerService htmlSanitizer,
            IFileService fileService)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _activityService = activityService;
            _logger = logger;
            _htmlSanitizer = htmlSanitizer;
        }

        public async Task<(IEnumerable<ProductDto> products, int count)> GetAllAsync(ProductRequestParameters p)
        {
            var result = await _manager.Product.GetAllAsync(p, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(result.products);

            return (productsDto, result.count);
        }

        public async Task<(IEnumerable<ProductDto> products, int count)> GetAllAdminAsync(ProductRequestParametersAdmin p, CancellationToken ct = default)
        {
            var result = await _manager.Product.GetAllAdminAsync(p, false, ct);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(result.products);

            return (productsDto, result.count);
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "products:count",
                async token => await _manager.Product.CountAsync(token),
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        private async Task<Product> GetByIdForServiceAsync(int productId, bool forUpdate, bool trackChanges)
        {
            var product = await _manager.Product.GetByIdAsync(productId, forUpdate, trackChanges);
            if (product == null)
                throw new ProductNotFoundException(productId);

            return product;
        }

        public async Task<ProductWithDetailsDto> GetByIdAsync(int productId, bool forUpdate = false)
        {
            var product = await GetByIdForServiceAsync(productId, forUpdate, false);
            var productDto = _mapper.Map<ProductWithDetailsDto>(product);

            return productDto;
        }

        private async Task<ProductVariant> GetVariantByIdForServiceAsync(int variantId, bool includeImages, bool trackChanges)
        {
            var variant = await _manager.ProductVariant.GetByIdAsync(variantId, includeImages, trackChanges);
            if (variant == null)
                throw new ProductVariantNotFoundException(variantId);

            return variant;
        }

        public async Task<ProductVariantDto> GetVariantByIdAsync(int variantId, bool includeImages)
        {
            var variant = await GetVariantByIdForServiceAsync(variantId, includeImages, false);
            var variantDto = _mapper.Map<ProductVariantDto>(variant);

            return variantDto;
        }

        public async Task<ProductWithDetailsDto> GetBySlugAsync(string slug)
        {
            var product = await _manager.Product.GetBySlugAsync(slug, false);
            if (product == null) throw new ProductNotFoundExceptionForSlug(slug);

            FilterVariantSelectors(product);
            await LoadDefaultVariantDetails(product);

            return product;
        }

        private void FilterVariantSelectors(ProductWithDetailsDto product)
        {
            if (product.VariantSelectors?.Count == 0 || product.Variants.Count <= 1)
            {
                product.VariantSelectors = new List<ProductVariantSelectorDto>();
                return;
            }

            var commonKeys = product.Variants
                .SelectMany(v => v.VariantSpecifications
                    .Select(v => v.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                )
                .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() == product.Variants.Count)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var hasColorVariant = product.Variants.Any(v => !string.IsNullOrWhiteSpace(v.Color));
            var hasSizeVariant = product.Variants.Any(v => !string.IsNullOrWhiteSpace(v.Size));

            if (hasColorVariant && !commonKeys.Contains("Renk"))
            {
                commonKeys.Add("Renk");
            }

            if (hasSizeVariant && !commonKeys.Contains("Beden"))
            {
                commonKeys.Add("Beden");
            }

            product.VariantSelectors = product.VariantSelectors!
                .Where(s => commonKeys.Contains(s.Key))
                .ToList();
        }

        private async Task LoadDefaultVariantDetails(ProductWithDetailsDto product)
        {
            var defaultVariantId = product.Variants.FirstOrDefault(v => v.IsDefault)?.ProductVariantId;
            if (defaultVariantId == null)
                return;

            var defaultVariantFull = await _manager.ProductVariant.GetByIdAsync(defaultVariantId.Value, true, false);
            if (defaultVariantFull == null)
                throw new ProductVariantNotFoundException(defaultVariantId.Value);

            var defaultVariant = product.Variants.First(v => v.ProductVariantId == defaultVariantId);
            _mapper.Map(defaultVariantFull, defaultVariant);
        }

        public async Task<IEnumerable<ProductDto>> GetRecommendationsAsync()
        {
            var products = await _manager.Product.GetRecommendationsAsync(false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetFavouritesAsync(FavouriteResultDto favouritesDto)
        {
            var products = await _manager.Product.GetFavouritesAsync(favouritesDto.FavouriteProductVariantsId, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetLatestAsync(int count)
        {
            var products = await _manager.Product.GetLatestAsync(count, false);
            var productsDto = _mapper.Map<IEnumerable<ProductDto>>(products);

            return productsDto;
        }

        public async Task<IEnumerable<ProductDto>> GetShowcaseListAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "products:showcase",
                async token =>
                {
                    var products = await _manager.Product.GetShowcaseListAsync(false, token);
                    return _mapper.Map<IEnumerable<ProductDto>>(products);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<OperationResult<ProductWithDetailsDto>> CreateAsync(ProductDtoForCreation productDto)
        {
            try
            {
                var product = _mapper.Map<Product>(productDto);

                if (!string.IsNullOrWhiteSpace(product.LongDescription))
                    product.LongDescription = _htmlSanitizer.Sanitize(product.LongDescription);

                product.Slug = !string.IsNullOrWhiteSpace(productDto.Slug)
                    ? SlugHelper.GenerateSlug(productDto.Slug)
                    : SlugHelper.GenerateSlug(product.ProductName);

                var existingCount = await _manager.Product.CountBySlugAsync(product.Slug);
                if (existingCount > 0)
                    product.Slug = SlugHelper.MakeUnique(product.Slug, existingCount + 1);

                if (string.IsNullOrWhiteSpace(product.MetaTitle))
                    product.MetaTitle = SeoMetaHelper.GenerateProductMetaTitle(product.ProductName, productDto.Brand);

                if (string.IsNullOrWhiteSpace(product.MetaDescription))
                    product.MetaDescription = SeoMetaHelper.GenerateProductMetaDescription(
                        product.ProductName,
                        productDto.Summary,
                        product.MinPrice,
                        productDto.Brand);

                await EnsureCategoryAttributesExist(productDto.CategoryId, productDto.Variants, productDto.NewAttributeDefinitions);

                var attributes = await _manager.CategoryVariantAttribute.GetByCategoryIdAsync(product.CategoryId, false);

                foreach (var variant in product.Variants)
                {
                    variant.ValidateStock();

                    variant.CombinationKey = GenerateCombinationKey(variant, attributes);

                    if (string.IsNullOrWhiteSpace(variant.CombinationKey))
                        throw new ProductValidationException("Varyant belirleyici alanlar boş olamaz.");

                    var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                    variant.CreatedByUserId = userId;
                    variant.UpdatedByUserId = userId;
                    variant.CreatedAt = DateTime.UtcNow;
                    variant.UpdatedAt = DateTime.UtcNow;
                }

                product.ValidateForCreation();

                ValidateVariantConsistency(product.Variants, attributes);

                var duplicateKeys = product.Variants
                    .GroupBy(v => v.CombinationKey)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateKeys.Any())
                    throw new ProductValidationException("Aynı varyant kombinasyonu birden fazla oluşturulamaz.");

                var userIdLog = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                product.CreatedByUserId = userIdLog;
                product.UpdatedByUserId = userIdLog;

                _manager.Product.Create(product);
                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userIdLog,
                    userName: userName,
                    action: "Create",
                    entityName: "Product",
                    entityId: product.ProductId.ToString(),
                    newValues: new
                    {
                        product.ProductName,
                        product.MinPrice,
                        product.MaxPrice,
                        TotalStock = product.TotalStock,
                        product.CategoryId,
                        VariantCount = product.Variants.Count
                    }
                );

                await _activityService.LogAsync(
                    "Yeni Ürün",
                    $"{product.ProductName} ürünü eklendi.",
                    "fa-box",
                    "text-blue-500 bg-blue-100",
                    $"/admin/products/update/{product.ProductId}"
                );

                _logger.LogInformation(
                    "Product created successfully. ProductId: {ProductId}, Name: {ProductName}, User: {UserId}",
                    product.ProductId, product.ProductName, userIdLog);

                var productWithDetailsDto = _mapper.Map<ProductWithDetailsDto>(product);

                await _cache.RemoveByPrefixAsync("products:");
                return OperationResult<ProductWithDetailsDto>.Success(productWithDetailsDto, "Ürün başarıyla oluşturuldu.");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning(ex, "Product validation failed. ProductName: {ProductName}", productDto.ProductName);
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
            catch (InvalidPriceException ex)
            {
                _logger.LogWarning(ex, "Invalid price.");
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateImagesAsync(IEnumerable<ProductImageDtoForCreation> productImagesDto)
        {
            try
            {
                if (productImagesDto == null || !productImagesDto.Any()) return OperationResult<ProductWithDetailsDto>.Success("Resim listesi boş.");

                var variantId = productImagesDto.First().ProductVariantId;
                var productVariant = await GetVariantByIdForServiceAsync(variantId, true, true);

                if (productVariant.Images != null && productVariant.Images.Any())
                {
                     var existingImages = productVariant.Images.ToList();
                     foreach(var img in existingImages)
                     {
                         _manager.Product.DeleteImage(img);
                     }
                     productVariant.Images.Clear();
                }

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var newImages = _mapper.Map<IEnumerable<ProductImage>>(productImagesDto);

                if (productVariant.Images == null) productVariant.Images = new List<ProductImage>();

                foreach (var img in newImages)
                {
                    img.ValidateForCreation();

                    img.CreatedAt = DateTime.UtcNow;
                    img.CreatedByUserId = userId;
                    img.UpdatedAt = DateTime.UtcNow;
                    img.UpdatedByUserId = userId;
                    
                    img.ProductVariantId = productVariant.ProductVariantId;

                    _manager.Product.CreateImage(img);
                    productVariant.Images.Add(img);
                }

                await _manager.SaveAsync();

                _logger.LogInformation(
                    "Product images overwritten. ProductId: {ProductId}, ProductVariantId: {ProductVariantId}, NewImageCount: {Count}",
                    productVariant.ProductId, productVariant.ProductVariantId, productImagesDto.Count());

                return OperationResult<ProductWithDetailsDto>.Success("Ürün resimleri başarıyla güncellendi.");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning(ex, "Product image validation failed. ProductId: {ProductId}, ProductVariantId: {ProductVariantId}",
                    productImagesDto?.FirstOrDefault()?.ProductId, productImagesDto?.FirstOrDefault()?.ProductVariantId);
                return OperationResult<ProductWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateShowcaseStatus(int productId)
        {
            _manager.ClearTracker();
            var product = await GetByIdForServiceAsync(productId, true, true);
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

            await _cache.RemoveAsync("products:showcase");
            return OperationResult<ProductWithDetailsDto>.Success(
                productDto,
                product.ShowCase ? "Ürün vitrine eklendi." : "Ürün vitrinden kaldırıldı.");
        }

        public async Task<OperationResult<ProductWithDetailsDto>> UpdateAsync(ProductDtoForUpdate productDto)
        {
            try
            {
                _manager.ClearTracker();
                var product = await GetByIdForServiceAsync(productDto.ProductId, true, true);

                var oldValues = new
                {
                    product.ProductName,
                    product.MinPrice,
                    product.MaxPrice,
                    product.TotalStock
                };

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                _mapper.Map(productDto, product);

                if (!string.IsNullOrWhiteSpace(product.LongDescription))
                    product.LongDescription = _htmlSanitizer.Sanitize(product.LongDescription);

                if (!string.IsNullOrWhiteSpace(productDto.Slug))
                {
                    product.Slug = SlugHelper.GenerateSlug(productDto.Slug);
                }
                else
                {
                    product.Slug = SlugHelper.GenerateSlug(product.ProductName);
                }

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
                        product.Summary,
                        product.MinPrice,
                        productDto.Brand);
                }

                if (string.IsNullOrWhiteSpace(product.MetaDescription))
                {
                    product.MetaDescription = SeoMetaHelper.GenerateProductMetaDescription(
                        product.ProductName,
                        product.Summary,
                        product.MinPrice,
                        productDto.Brand);
                }

                _mapper.Map(productDto, product);

                await EnsureCategoryAttributesExist(product.CategoryId, productDto.Variants, productDto.NewAttributeDefinitions);

                var attributes = await _manager.CategoryVariantAttribute.GetByCategoryIdAsync(product.CategoryId, false);

                if (productDto.Variants != null && productDto.Variants.Any())
                {
                    var existingVariants = await _manager.ProductVariant.GetByProductIdAsync(product.ProductId, true);
                    var existingVariantsList = existingVariants.ToList();

                    var incomingVariantIds = productDto.Variants
                        .Where(v => v.ProductVariantId > 0)
                        .Select(v => v.ProductVariantId)
                        .ToList();

                    var variantsToDelete = existingVariantsList
                        .Where(ev => !incomingVariantIds.Contains(ev.ProductVariantId))
                        .ToList();

                    foreach (var variantToDelete in variantsToDelete)
                    {
                        _manager.ProductVariant.Delete(variantToDelete);
                        _logger.LogInformation("Variant deleted. ProductVariantId: {VariantId}", variantToDelete.ProductVariantId);
                    }

                    foreach (var variantDto in productDto.Variants)
                    {
                        ProductVariant? targetVariant;
                        
                        if (variantDto.ProductVariantId > 0)
                        {
                            targetVariant = existingVariantsList
                                .FirstOrDefault(ev => ev.ProductVariantId == variantDto.ProductVariantId);

                            if (targetVariant != null)
                            {
                                targetVariant.Color = variantDto.Color;
                                targetVariant.Size = variantDto.Size;
                                targetVariant.Price = variantDto.Price;
                                targetVariant.DiscountPrice = variantDto.DiscountPrice;
                                targetVariant.Stock = variantDto.Stock;
                                targetVariant.Sku = variantDto.Sku;
                                targetVariant.Gtin = variantDto.Gtin;
                                targetVariant.IsActive = variantDto.IsActive;
                                targetVariant.IsDefault = variantDto.IsDefault;
                                targetVariant.UpdatedAt = DateTime.UtcNow;
                                targetVariant.VariantSpecificationsJson = JsonSerializer.Serialize(variantDto.VariantSpecifications);

                                targetVariant.WeightOverride = variantDto.WeightOverride;
                                targetVariant.LengthOverride = variantDto.LengthOverride;
                                targetVariant.WidthOverride = variantDto.WidthOverride;
                                targetVariant.HeightOverride = variantDto.HeightOverride;

                                _manager.ProductVariant.Update(targetVariant);
                                _logger.LogInformation("Variant updated. ProductVariantId: {VariantId}", targetVariant.ProductVariantId);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            targetVariant = new ProductVariant
                            {
                                ProductId = product.ProductId,
                                Color = variantDto.Color,
                                Size = variantDto.Size,
                                Price = variantDto.Price,
                                DiscountPrice = variantDto.DiscountPrice,
                                Stock = variantDto.Stock,
                                Sku = variantDto.Sku,
                                Gtin = variantDto.Gtin,
                                IsActive = variantDto.IsActive,
                                IsDefault = variantDto.IsDefault,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                VariantSpecificationsJson = JsonSerializer.Serialize(variantDto.VariantSpecifications),

                                WeightOverride = variantDto.WeightOverride,
                                LengthOverride = variantDto.LengthOverride,
                                WidthOverride = variantDto.WidthOverride,
                                HeightOverride = variantDto.HeightOverride
                            };
                            
                            _manager.ProductVariant.Create(targetVariant);
                            
                            _logger.LogInformation("New variant added. Color: {Color}, Size: {Size}", targetVariant.Color, targetVariant.Size);
                        }

                        targetVariant.ValidateStock();
                        targetVariant.CombinationKey = GenerateCombinationKey(targetVariant, attributes);

                        if (string.IsNullOrWhiteSpace(targetVariant.CombinationKey))
                             throw new ProductValidationException("Varyant belirleyici alanlar boş olamaz.");

                        targetVariant.UpdatedByUserId = userId;
                        if(targetVariant.ProductVariantId == 0) targetVariant.CreatedByUserId = userId;
                    }
                }

                product.ValidateForCreation();

                 var duplicateKeys = (productDto.Variants ?? Enumerable.Empty<ProductVariantDtoForCreation>())
                    .Select(v => {
                        return 0; 
                    }).ToList();
                
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
                    }
                );

                _logger.LogInformation(
                    "Product updated successfully. ProductId: {ProductId}, User: {UserId}",
                    product.ProductId, userId);

                await _cache.RemoveAsync("products:showcase");
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

        public async Task<OperationResult<ProductWithDetailsDto>> DeleteAsync(int productId)
        {
            var product = await GetByIdForServiceAsync(productId, true, true);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            product.SoftDelete(userId);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "Product",
                entityId: productId.ToString()
            );

            _logger.LogInformation(
                "Product soft deleted. ProductId: {ProductId}, User: {UserId}",
                productId, userId);

            await _cache.RemoveByPrefixAsync("products:");
            return OperationResult<ProductWithDetailsDto>.Success("Ürün başarıyla silindi.");
        }

        private async Task EnsureCategoryAttributesExist(int categoryId, IEnumerable<ProductVariantDtoForCreation> variants, List<CategoryVariantAttributeDtoForCreation>? newAttributeDefinitions)
        {
            if (newAttributeDefinitions != null && newAttributeDefinitions.Any())
            {
                foreach (var attrDef in newAttributeDefinitions)
                {
                    var exists = await _manager.CategoryVariantAttribute.ExistsByKeyAsync(attrDef.Key, categoryId);
                    if (!exists)
                    {
                        var newAttr = new CategoryVariantAttribute
                        {
                            CategoryId = categoryId,
                            Key = attrDef.Key,
                            DisplayName = attrDef.DisplayName,
                            Type = attrDef.Type,
                            IsVariantDefiner = attrDef.IsVariantDefiner,
                            IsTechnicalSpec = attrDef.IsTechnicalSpec,
                            SortOrder = attrDef.SortOrder,
                            IsRequired = attrDef.IsRequired
                        };
                        _manager.CategoryVariantAttribute.Create(newAttr);
                    }
                }
                await _manager.SaveAsync();
            }

            if (variants == null || !variants.Any()) return;

            var allKeysUsed = variants
                .SelectMany(v => v.VariantSpecifications.Select(s => s.Key))
                .Distinct()
                .ToList();

            if (variants.Any(v => !string.IsNullOrEmpty(v.Color))) allKeysUsed.Add("Renk");
            if (variants.Any(v => !string.IsNullOrEmpty(v.Size))) allKeysUsed.Add("Beden");

            allKeysUsed = allKeysUsed.Distinct().ToList();

            if (!allKeysUsed.Any()) return;

            var existingAttributes = await _manager.CategoryVariantAttribute.GetByCategoryIdAsync(categoryId, false);
            var existingKeys = existingAttributes.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var totallyNewKeys = allKeysUsed.Where(k => !existingKeys.Contains(k)).ToList();

            if (totallyNewKeys.Any())
            {
                foreach (var key in totallyNewKeys)
                {
                    var newAttr = new CategoryVariantAttribute
                    {
                        CategoryId = categoryId,
                        Key = key,
                        DisplayName = key,
                        Type = VariantAttributeType.Select,
                        IsVariantDefiner = true,
                        IsTechnicalSpec = false,
                        SortOrder = 0
                    };
                    _manager.CategoryVariantAttribute.Create(newAttr);
                }
                await _manager.SaveAsync();
            }
        }

        private void ValidateVariantConsistency(ICollection<ProductVariant> variants, IEnumerable<CategoryVariantAttribute> attributes)
        {
            if (variants.Count == 0) return;

            var requiredAttributes = attributes.Where(a => a.IsRequired).ToList();

            foreach (var attr in requiredAttributes)
            {
                foreach (var variant in variants)
                {
                    var value = GetAttributeValue(variant, attr.Key);

                    if (string.IsNullOrWhiteSpace(value))
                        throw new ProductValidationException(
                            $"'{attr.DisplayName}' zorunludur ve tüm varyantlarda girilmelidir.");
                }
            }

            var definers = attributes
                .Where(a => a.IsVariantDefiner && !a.IsRequired)
                .ToList();

            foreach (var definer in definers)
            {
                var filledCount = variants.Count(v =>
                    !string.IsNullOrWhiteSpace(GetAttributeValue(v, definer.Key)));

                if (filledCount > 0 && filledCount < variants.Count)
                {
                    throw new ProductValidationException(
                        $"'{definer.DisplayName}' ya tüm varyantlarda olmalı ya da hiçbirinde olmamalı.");
                }
            }
        }

        private string GenerateCombinationKey(ProductVariant variant, IEnumerable<CategoryVariantAttribute> attributes)
        {
            var definers = attributes
                .Where(a => a.IsVariantDefiner)
                .OrderBy(a => a.SortOrder)
                .ToList();

            var parts = new List<string>();

            foreach (var attr in definers)
            {
                var value = GetAttributeValue(variant, attr.Key);

                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{attr.Key}:{value.Trim()}");
            }

            return string.Join("|", parts);
        }

        private string? GetAttributeValue(ProductVariant variant, string key)
        {
            if (key.Equals("Renk", StringComparison.OrdinalIgnoreCase))
                return variant.Color;

            if (key.Equals("Beden", StringComparison.OrdinalIgnoreCase))
                return variant.Size;

            if (string.IsNullOrEmpty(variant.VariantSpecificationsJson))
                return null;

            var specs = JsonSerializer.Deserialize<List<ProductSpecificationDto>>(variant.VariantSpecificationsJson);

            return specs?
                .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }
    }
}
