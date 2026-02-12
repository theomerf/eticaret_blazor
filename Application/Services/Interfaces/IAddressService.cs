using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IAddressService
    {
        Task<IEnumerable<AddressDto>> GetAllAsync();
        Task<AddressDto> GetByIdAsync(int addressId);
        Task<IEnumerable<AddressDto>> GetByUserIdAsync(string userId);
        Task<OperationResult<AddressDto>> CreateAsync(AddressDtoForCreation addressDto);
        Task<OperationResult<AddressDto>> MakeDefaultAsync(int addressId);
        Task<OperationResult<AddressDto>> UpdateAsync(AddressDtoForUpdate addressDto);
        Task<OperationResult<AddressDto>> DeleteAsync(int addressId);
    }
}
