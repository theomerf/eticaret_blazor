using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<(IEnumerable<UserDto> users, int count, int activeCount)> GetAllUsersAdminAsync(UserRequestParametersAdmin p, CancellationToken ct = default);
        Task<int> GetUsersCountAsync(CancellationToken ct = default);
        Task<IEnumerable<Role>> GetRolesAsync(CancellationToken ct = default);
        Task<int> GetRolesCountAsync(CancellationToken ct = default);
        Task<UserDto> GetOneUserAsync(string userId, CancellationToken ct = default);
        Task<OperationResult<UserDto>> CreateUserAsync(UserDtoForCreation userDto);
        Task<OperationResult<UserDto>> UpdateUserAsync(UserDtoForUpdate userDtoForUpdate);
        Task<OperationResult<UserDto>> UpdateUserForAdminAsync(UserDtoForUpdateAdmin userDtoForUpdate);
        Task<OperationResult<UserDto>> DeleteUserAsync(string userId);
        Task<FavouriteResultDto> GetOneUsersFavouritesAsync(string userId);
        Task<OperationResult<FavouriteResultDto>> AddToFavouritesAsync(int productId);
        Task<OperationResult<FavouriteResultDto>> RemoveFromFavouritesAsync(int productId);
        Task<OperationResult<FavouriteResultDto>> UpdateUserFavouritesAsync(List<int> favouriteProductIds);
        Task<OperationResult<UserDto>> ToggleUserActiveAsync(string userId);
        Task<OperationResult<UserDto>> ChangeUserRolesAsync(string userId, HashSet<string> roles);
        Task<OperationResult<UserDto>> UpdateAdminNotesAsync(string userId, string notes);
        Task<OperationResult<UserDto>> EditUserInfoAsync(UserDtoForAdminEdit dto);
    }
}
