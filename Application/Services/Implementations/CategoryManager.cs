using Application.Common.Helpers;
using Application.DTOs;
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
    public class CategoryManager : ICategoryService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CategoryManager> _logger;

        public CategoryManager(
            IRepositoryManager manager,
            IMapper mapper,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            ILogger<CategoryManager> logger)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(bool trackChanges)
        {
            string cacheKey = "allCategories";

            if (_cache.TryGetValue(cacheKey, out List<CategoryDto>? cachedCategories))
            {
                return cachedCategories ?? new List<CategoryDto>();
            }

            var categories = await _manager.Category.GetAllCategoriesAsync(trackChanges);
            var categoriesDto = _mapper.Map<IEnumerable<CategoryDto>>(categories).ToList();

            _cache.Set(cacheKey, categoriesDto,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                }
            );

            return categoriesDto;
        }

        public async Task<int> GetCategoriesCountAsync()
        {
            string cacheKey = "categoriesCount";

            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return cachedCount;
            }

            var count = await _manager.Category.GetCategoriesCountAsync();

            _cache.Set(cacheKey, count,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                }
            );

            return count;
        }

        private async Task<Category> GetOneCategoryForServiceAsync(int id, bool trackChanges)
        {
            var category = await _manager.Category.GetOneCategoryAsync(id, trackChanges);
            if (category == null)
                throw new CategoryNotFoundException(id);

            return category;
        }

        public async Task<CategoryWithDetailsDto> GetOneCategoryAsync(int id)
        {
            var category = await GetOneCategoryForServiceAsync(id, false);
            var categoryDto = _mapper.Map<CategoryWithDetailsDto>(category);

            return categoryDto;
        }

        public async Task<IEnumerable<CategoryDto>> GetParentCategoriesAsync()
        {
            var categories = await _manager.Category.GetParentCategoriesAsync(false);
            var categoriesDto = _mapper.Map<IEnumerable<CategoryDto>>(categories);

            return categoriesDto;
        }

        public async Task<IEnumerable<Category>> GetChildsOfOneCategoryAsync(int parentId)
        {
            var categories = await _manager.Category.GetChildsOfOneCategoryAsync(parentId, false);
            var categoriesDto = _mapper.Map<IEnumerable<Category>>(categories);

            return categoriesDto;
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> CreateCategoryAsync(CategoryDtoForCreation categoryDto)
        {
            try
            {
                var category = _mapper.Map<Category>(categoryDto);

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
                    var allCategories = await _manager.Category.GetAllCategoriesAsync(false);
                    if (category.HasCircularReference(category.ParentId.Value, allCategories))
                    {
                        throw new CategoryValidationException("Döngüsel referans tespit edildi. Kategori hiyerarşisi geçersiz.");
                    }
                }

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                _manager.Category.CreateCategory(category);
                category.CreatedByUserId = userId;
                category.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "Category",
                    entityId: category.CategoryId.ToString(),
                    newValues: new
                    {
                        category.CategoryName,
                        category.ParentId,
                        category.IsVisible,
                        category.DisplayOrder
                    }
                );

                _logger.LogInformation(
                    "Category created successfully. CategoryId: {CategoryId}, Name: {CategoryName}, User: {UserId}",
                    category.CategoryId, category.CategoryName, userId);

                return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla oluşturuldu.");
            }
            catch (CategoryValidationException ex)
            {
                _logger.LogWarning(ex, "Category validation failed. CategoryName: {CategoryName}", categoryDto.CategoryName);
                return OperationResult<CategoryWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> UpdateCategoryAsync(CategoryDtoForUpdate categoryDto)
        {
            try
            {
                var category = await GetOneCategoryForServiceAsync(categoryDto.CategoryId, true);

                var oldValues = new
                {
                    category.CategoryName,
                    category.ParentId,
                    category.IsVisible,
                    category.DisplayOrder
                };

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                _mapper.Map(categoryDto, category);

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

                category.ValidateForUpdate();

                if (category.ParentId.HasValue)
                {
                    var allCategories = await _manager.Category.GetAllCategoriesAsync(false);
                    if (category.HasCircularReference(category.ParentId.Value, allCategories))
                    {
                        throw new CategoryValidationException("Döngüsel referans tespit edildi. Kategori hiyerarşisi geçersiz.");
                    }
                }

                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "Category",
                    entityId: categoryDto.CategoryId.ToString(),
                    oldValues: oldValues,
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
                    category.CategoryId, userId);

                return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla güncellendi.");
            }
            catch (CategoryValidationException ex)
            {
                _logger.LogWarning(ex, "Category validation failed during update. CategoryId: {CategoryId}", categoryDto.CategoryId);
                return OperationResult<CategoryWithDetailsDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CategoryWithDetailsDto>> DeleteCategoryAsync(int id)
        {
            var category = await GetOneCategoryForServiceAsync(id, true);

            var childCategories = await _manager.Category.GetChildsOfOneCategoryAsync(id, false);
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

            return OperationResult<CategoryWithDetailsDto>.Success("Kategori başarıyla silindi.");
        }
    }
}
