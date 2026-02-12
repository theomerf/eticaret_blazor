using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartOperationResult> SetQuantityAsync(string? userId, int productId, int productVariantId, int newQuantity);
        Task<CartOperationResult> AddOrUpdateItemAsync(string? userId, int productId, int productVariantId, int quantity);
        Task<CartOperationResult> RemoveItemAsync(string? userId, int productId, int productVariantId);
        Task<CartOperationResult> ClearAsync(string? userId);
        Task<CartDto> GetByUserIdAsync(string? userId, bool validate = false);
        Task<int> CountOfLinesAsync(string userId);
        Task<int> GetVersionAsync(string userId);
        Task<bool> ValidateAsync(string? userId);
        Task<CartDto> MergeCartsAsync(string userId, CartDto sessionCart);
    }
}
