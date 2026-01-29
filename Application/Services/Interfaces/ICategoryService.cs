using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(bool trackChanges);
        Task<int> GetCategoriesCountAsync();
        Task<CategoryWithDetailsDto> GetOneCategoryAsync(int id);
        Task<IEnumerable<CategoryDto>> GetParentCategoriesAsync();
        Task<IEnumerable<Category>> GetChildsOfOneCategoryAsync(int parentId);
        Task<OperationResult<CategoryWithDetailsDto>> CreateCategoryAsync(CategoryDtoForCreation categoryDto);
        Task<OperationResult<CategoryWithDetailsDto>> UpdateCategoryAsync(CategoryDtoForUpdate categoryDto);
        Task<OperationResult<CategoryWithDetailsDto>> DeleteCategoryAsync(int id);
    }
}
