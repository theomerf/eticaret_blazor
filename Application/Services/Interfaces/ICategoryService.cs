using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<(IEnumerable<CategoryDto> categories, int count)> GetAllAdminAsync(RequestParametersAdmin p, CancellationToken ct);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<CategoryWithDetailsDto> GetByIdAsync(int id);
        Task<IEnumerable<CategoryDto>> GetParentsAsync();
        Task<IEnumerable<Category>> GetChildrenByIdAsync(int parentId);
        Task<OperationResult<CategoryWithDetailsDto>> CreateAsync(CategoryDtoForCreation categoryDto);
        Task<OperationResult<CategoryDto>> UpdateFeaturedStatus(int categoryId);
        Task<OperationResult<CategoryWithDetailsDto>> UpdateAsync(CategoryDtoForUpdate categoryDto);
        Task<OperationResult<CategoryWithDetailsDto>> DeleteAsync(int id);
        Task<IEnumerable<CategoryVariantAttributeDto>> GetAttributesAsync(int categoryId);
    }
}
