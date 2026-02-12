using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<(IEnumerable<ProductDto> products, int count)> GetAllAsync(ProductRequestParameters p);
        Task<(IEnumerable<ProductDto> products, int count)> GetAllAdminAsync(ProductRequestParametersAdmin p, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<ProductWithDetailsDto> GetByIdAsync(int productId, bool forUpdate = false);
        Task<ProductVariantDto> GetVariantByIdAsync(int variantId);
        Task<ProductWithDetailsDto> GetBySlugAsync(string slug);
        Task<IEnumerable<ProductDto>> GetRecommendationsAsync();
        Task<IEnumerable<ProductDto>> GetFavouritesAsync(FavouriteResultDto favouritesDto);
        Task<IEnumerable<ProductDto>> GetLatestAsync(int count);
        Task<IEnumerable<ProductDto>> GetShowcaseListAsync(CancellationToken ct = default);
        Task<OperationResult<ProductWithDetailsDto>> UpdateImagesAsync(IEnumerable<ProductImageDtoForCreation> productImagesDto);
        Task<OperationResult<ProductWithDetailsDto>> CreateAsync(ProductDtoForCreation productDto);
        Task<OperationResult<ProductWithDetailsDto>> UpdateShowcaseStatus(int productId);
        Task<OperationResult<ProductWithDetailsDto>> UpdateAsync(ProductDtoForUpdate productDto);
        Task<OperationResult<ProductWithDetailsDto>> DeleteAsync(int productId);
    }
}
