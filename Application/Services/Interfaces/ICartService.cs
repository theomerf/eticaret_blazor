using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartOperationResult> SetQuantityAsync(string? userId, int productId, int newQuantity);
        Task<CartOperationResult> AddOrUpdateItemAsync(string? userId, int productId, int quantity);
        Task<CartOperationResult> RemoveItemAsync(string? userId, int productId);
        Task<CartDto> GetCartAsync(string? userId, bool validate = false);
        Task<int> GetCartLinesCountAsync(string userId);
        Task<int> GetCartVersionAsync(string userId);
        Task<bool> ValidateCartAsync(string? userId);
        Task<CartDto> MergeCartsAsync(string userId, CartDto sessionCart);
    }
}
