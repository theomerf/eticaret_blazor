using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        IEnumerable<IdentityRole> Roles { get; }
        Task<int> GetUsersCountAsync();
        Task<UserDto> GetOneUserAsync(string userId);
        Task<OperationResult<UserDto>> ResetPasswordAsync(ResetPasswordDto model);
        Task<OperationResult<UserDto>> ChangePasswordAsync(ChangePasswordDto model);
        Task<OperationResult<UserDto>> CreateUserAsync(UserDtoForCreation userDto);
        Task<OperationResult<UserDto>> UpdateUserAsync(UserDtoForUpdate userDtoForUpdate);
        Task<OperationResult<UserDto>> UpdateUserForAdminAsync(UserDtoForUpdateAdmin userDtoForUpdate);
        Task<OperationResult<UserDto>> DeleteUserAsync(string userId);
        Task<FavouriteResultDto> GetOneUsersFavouritesAsync(string userId);
        Task<OperationResult<FavouriteResultDto>> AddToFavouritesAsync(int productId);
        Task<OperationResult<FavouriteResultDto>> RemoveFromFavouritesAsync(int productId);
        Task<OperationResult<FavouriteResultDto>> UpdateUserFavouritesAsync(List<int> favouriteProductIds);
    }
}
