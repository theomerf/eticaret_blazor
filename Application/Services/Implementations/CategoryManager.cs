using Application.Common.Helpers;
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

namespace Application.Services.Implementations
{
    public class CategoryManager : ICategoryService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly IAuditLogService _auditLogService;
        private readonly IActivityService _activityService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CategoryManager> _logger;

        public CategoryManager(
            IRepositoryManager manager,
            IMapper mapper,
            ICacheService cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            IActivityService activityService,
            ILogger<CategoryManager> logger)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "categories:list",
                async token =>
                {
                    var categories = await _manager.Category.GetAllAsync(false, token);
                    return _mapper.Map<IEnumerable<CategoryDto>>(categories).ToList();
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<(IEnumerable<CategoryDto> categories, int count, int featuredCount)> GetAllAdminAsync(RequestParametersAdmin p, CancellationToken ct = default)
        {
            var result = await _manager.Category.GetAllAdminAsync(p, false, ct);
            var categoriesDto = _mapper.Map<IEnumerable<CategoryDto>>(result.categories);

            return (categoriesDto, result.count, result.featuredCount);
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "categories:count",
                async token =>
                {
                    return await _manager.Category.CountAsync(token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        private async Task<Category> GetByIdForServiceAsync(int id, bool trackChanges)
        {
            var category = await _manager.Category.GetByIdAsync(id, trackChanges);
            if (category == null)
                throw new CategoryNotFoundException(id);

            return category;
        }

        public async Task<CategoryWithDetailsDto> GetByIdAsync(int id)
        {
            var category = await GetByIdForServiceAsync(id, false);
            var categoryDto = _mapper.Map<CategoryWithDetailsDto>(category);

            return categoryDto;
        }

        public async Task<IEnumerable<CategoryDto>> GetParentsAsync()
        {
            var categories = await _manager.Category.GetParentsAsync(false);
            var categoriesDto = _mapper.Map<IEnumerable<CategoryDto>>(categories);

            return categoriesDto;
        }

        public async Task<IEnumerable<Category>> GetChildrenByIdAsync(int parentId)
        {
            var categories = await _manager.Category.GetChildrenByIdAsync(parentId, false);
            var categoriesDto = _mapper.Map<IEnumerable<Category>>(categories);

            return categoriesDto;
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> CreateAsync(CategoryDtoForCreation categoryDto)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
                Category? category = null;

                await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    category = _mapper.Map<Category>(categoryDto);

                    category.Slug = SlugHelper.GenerateSlug(category.CategoryName);

                    var existingCount = await _manager.Category.CountBySlugAsync(category.Slug);
                    if (existingCount > 0)
                    {
                        category.Slug = SlugHelper.MakeUnique(category.Slug, existingCount + 1);
                    }

                    if (string.IsNullOrWhiteSpace(category.MetaTitle))
                    {
                        category.MetaTitle = SeoMetaHelper.GenerateCategoryMetaTitle(category.CategoryName);
                    }

                    if (string.IsNullOrWhiteSpace(category.MetaDescription))
                    {
                        category.MetaDescription = SeoMetaHelper.GenerateCategoryMetaDescription(
                            category.CategoryName,
                            categoryDto.Description);
                    }

                    category.ValidateForCreation();

                    if (category.ParentId.HasValue)
                    {
                        var allCategories = await _manager.Category.GetAllAsync(false);
                        if (category.HasCircularReference(category.ParentId.Value, allCategories))
                        {
                            throw new CategoryValidationException("Döngüsel referans tespit edildi. Kategori hiyerarşisi geçersiz.");
                        }
                    }

                    category.CreatedByUserId = userId;
                    category.UpdatedByUserId = userId;

                    _manager.Category.Create(category);
                    await _manager.SaveAsync();

                    if (categoryDto.NewAttributes != null && categoryDto.NewAttributes.Any())
                    {
                        foreach (var attrDto in categoryDto.NewAttributes)
                        {
                            var exists = await _manager.CategoryVariantAttribute.ExistsByKeyAsync(attrDto.Key, category.CategoryId);
                            if (!exists)
                            {
                                var attribute = _mapper.Map<CategoryVariantAttribute>(attrDto);
                                attribute.CategoryId = category.CategoryId;
                                _manager.CategoryVariantAttribute.Create(attribute);
                            }
                        }

                        await _manager.SaveAsync();
                    }
                });

                if (category == null)
                {
                    return OperationResult<CategoryWithDetailsDto>.Failure("Kategori oluşturulamadı.", ResultType.Error);
                }
                
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "Category",
                    entityId: category!.CategoryId.ToString(),
                    newValues: new
                    {
                        category!.CategoryName,
                        category!.ParentId,
                        category!.IsVisible,
                        category!.DisplayOrder
                    }
                );

                await _activityService.LogAsync(
                    "Yeni Kategori",
                    $"{category!.CategoryName} kategorisi oluşturuldu.",
                    "fa-folder-plus",
                    "text-indigo-500 bg-indigo-100",
                    $"/admin/categories/edit/{category!.CategoryId}"
                );

                _logger.LogInformation(
                    "Category created successfully. CategoryId: {CategoryId}, Name: {CategoryName}, User: {UserId}",
                    category!.CategoryId, category!.CategoryName, userId);

                await _cache.RemoveByPrefixAsync("categories:");
                return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla oluşturuldu.");
            }
            catch (CategoryValidationException ex)
            {
                _logger.LogWarning(ex, "Category validation failed. CategoryName: {CategoryName}", categoryDto.CategoryName);
                return OperationResult<CategoryWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CategoryDto>> UpdateFeaturedStatus(int categoryId)
        {
            _manager.ClearTracker();
            var category = await GetByIdForServiceAsync(categoryId, true);
            var oldFeatured = category.IsFeatured;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            category.IsFeatured = !category.IsFeatured;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Update",
                entityName: "Category",
                entityId: category.CategoryId.ToString(),
                oldValues: new { IsFeatured = oldFeatured },
                newValues: new { category.IsFeatured }
            );

            _logger.LogInformation(
                "Category featured status updated. CategoryId: {CategoryId}, Featured: {Featured}",
                category.CategoryId, category.IsFeatured);

            var categoryDto = _mapper.Map<CategoryDto>(category);

            await _cache.RemoveAsync("products:showcase");
            return OperationResult<CategoryDto>.Success(
                categoryDto,
                category.IsFeatured ? "Kategori öne çıkarıldı." : "Kategori artık öne çıkarılmıyor.");
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> UpdateAsync(CategoryDtoForUpdate categoryDto)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
                (Category category, object oldValues) txResult = default;

                txResult = await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    _manager.ClearTracker();
                    var category = await GetByIdForServiceAsync(categoryDto.CategoryId, true);

                    var oldValues = new
                    {
                        category.CategoryName,
                        category.ParentId,
                        category.IsVisible,
                        category.DisplayOrder
                    };
                    var previousSlug = category.Slug;

                    _mapper.Map(categoryDto, category);

                    category.Slug = SlugHelper.GenerateSlug(category.CategoryName);

                    if (!string.Equals(previousSlug, category.Slug, StringComparison.OrdinalIgnoreCase))
                    {
                        var existingCount = await _manager.Category.CountBySlugAsync(category.Slug);
                        if (existingCount > 0)
                        {
                            category.Slug = SlugHelper.MakeUnique(category.Slug, existingCount + 1);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(category.MetaTitle))
                    {
                        category.MetaTitle = SeoMetaHelper.GenerateCategoryMetaTitle(category.CategoryName);
                    }

                    if (string.IsNullOrWhiteSpace(category.MetaDescription))
                    {
                        category.MetaDescription = SeoMetaHelper.GenerateCategoryMetaDescription(
                            category.CategoryName,
                            categoryDto.Description);
                    }

                    category.ValidateForUpdate();

                    if (category.ParentId.HasValue)
                    {
                        var allCategories = await _manager.Category.GetAllAsync(false);
                        if (category.HasCircularReference(category.ParentId.Value, allCategories))
                        {
                            throw new CategoryValidationException("Döngüsel referans tespit edildi. Kategori hiyerarşisi geçersiz.");
                        }
                    }

                    category.UpdatedAt = DateTime.UtcNow;
                    category.UpdatedByUserId = userId;

                    if (categoryDto.Attributes != null)
                    {
                        var existingAttributes = await _manager.CategoryVariantAttribute.GetByCategoryIdAsync(category.CategoryId, true);

                        var incomingIds = categoryDto.Attributes.Where(a => a.VariantAttributeId > 0).Select(a => a.VariantAttributeId).ToList();
                        var attributesToDelete = existingAttributes.Where(e => !incomingIds.Contains(e.VariantAttributeId)).ToList();

                        foreach (var attr in attributesToDelete)
                        {
                            _manager.CategoryVariantAttribute.Delete(attr);
                        }

                        var attributesToUpdate = categoryDto.Attributes.Where(a => a.VariantAttributeId > 0).ToList();
                        foreach (var updateDto in attributesToUpdate)
                        {
                            var existingAttr = existingAttributes.FirstOrDefault(e => e.VariantAttributeId == updateDto.VariantAttributeId);
                            if (existingAttr != null)
                            {
                                _mapper.Map(updateDto, existingAttr);
                            }
                        }

                        var attributesToAdd = categoryDto.Attributes.Where(a => a.VariantAttributeId == 0).ToList();
                        foreach (var addDto in attributesToAdd)
                        {
                            var exists = await _manager.CategoryVariantAttribute.ExistsByKeyAsync(addDto.Key, category.CategoryId);
                            if (!exists)
                            {
                                var newAttr = _mapper.Map<CategoryVariantAttribute>(addDto);
                                newAttr.CategoryId = category.CategoryId;
                                _manager.CategoryVariantAttribute.Create(newAttr);
                            }
                        }
                    }

                    await _manager.SaveAsync();

                    return (category, (object)oldValues);
                });

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "Category",
                    entityId: categoryDto.CategoryId.ToString(),
                    oldValues: txResult.oldValues,
                    newValues: new
                    {
                        categoryDto.CategoryName,
                        categoryDto.ParentId,
                        categoryDto.IsVisible,
                        categoryDto.DisplayOrder
                    }
                );

                _logger.LogInformation(
                    "Category updated successfully. CategoryId: {CategoryId}, User: {UserId}",
                    txResult.category.CategoryId, userId);

                await _cache.RemoveAsync("categories:list");
                return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla güncellendi.");
            }
            catch (CategoryValidationException ex)
            {
                _logger.LogWarning(ex, "Category validation failed during update. CategoryId: {CategoryId}", categoryDto.CategoryId);
                return OperationResult<CategoryWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> DeleteAsync(int id)
        {
            var category = await GetByIdForServiceAsync(id, true);

            var childCategories = await _manager.Category.GetChildrenByIdAsync(id, false);
            if (childCategories.Any())
            {
                _logger.LogWarning("Cannot delete category with children. CategoryId: {CategoryId}, ChildCount: {Count}",
                    id, childCategories.Count());
                return OperationResult<CategoryWithDetailsDto>.Failure(
                    "Alt kategorileri olan bir kategori silinemez.", ResultType.ValidationError);
            }

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            category.SoftDelete(userId);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "Category",
                entityId: id.ToString()
            );

            _logger.LogInformation(
                "Category soft deleted. CategoryId: {CategoryId}, User: {UserId}",
                id, userId);

            await _cache.RemoveByPrefixAsync("categories:");
            return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla silindi.");
        }

        public async Task<IEnumerable<CategoryVariantAttributeDto>> GetAttributesAsync(int categoryId)
        {
            var attributes = await _manager.CategoryVariantAttribute.GetByCategoryIdAsync(categoryId, false);
            return _mapper.Map<IEnumerable<CategoryVariantAttributeDto>>(attributes);
        }
    }
}
