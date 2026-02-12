using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync(bool trackChanges);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<CategoryWithDetailsDto> GetByIdAsync(int id);
        Task<IEnumerable<CategoryDto>> GetParentsAsync();
        Task<IEnumerable<Category>> GetChildrenByIdAsync(int parentId);
        Task<OperationResult<CategoryWithDetailsDto>> CreateAsync(CategoryDtoForCreation categoryDto);
        Task<OperationResult<CategoryWithDetailsDto>> UpdateAsync(CategoryDtoForUpdate categoryDto);
        Task<OperationResult<CategoryWithDetailsDto>> DeleteAsync(int id);
        Task<IEnumerable<CategoryVariantAttributeDto>> GetAttributesAsync(int categoryId);
    }
}
