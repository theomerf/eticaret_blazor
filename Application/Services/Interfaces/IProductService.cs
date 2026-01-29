using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<(IEnumerable<ProductDto> products, int count)> GetAllProductsAsync(ProductRequestParameters p);
        Task<(IEnumerable<ProductDto> products, int count)> GetAllProductsAdminAsync(ProductRequestParametersAdmin p);
        Task<int> GetProductsCountAsync();
        Task<ProductWithDetailsDto> GetOneProductAsync(int id);
        Task<ProductWithDetailsDto> GetOneProductBySlugAsync(string slug);
        Task<IEnumerable<ProductDto>> GetRecommendedProductsAsync();
        Task<IEnumerable<ProductDto>> GetFavouriteProductsAsync(FavouriteResultDto favouritesDto);
        Task<IEnumerable<ProductDto>> GetLastestProductsAsync(int n);
        Task<IEnumerable<ProductDto>> GetShowcaseProductsAsync();
        Task<OperationResult<ProductWithDetailsDto>> UpdateProductImagesAsync(IEnumerable<ProductImageDtoForCreation> productImagesDto);
        Task<OperationResult<int>> CreateProductAsync(ProductDtoForCreation productDto);
        Task<OperationResult<ProductWithDetailsDto>> UpdateProductShowcaseStatus(int id);
        Task<OperationResult<ProductWithDetailsDto>> UpdateProductAsync(ProductDtoForUpdate productDto);
        Task<OperationResult<ProductWithDetailsDto>> DeleteProductAsync(int id);
    }
}
