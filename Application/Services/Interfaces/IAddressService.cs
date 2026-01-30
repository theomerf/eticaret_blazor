using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IAddressService
    {
        Task<IEnumerable<AddressDto>> GetAllAddressesAsync();
        Task<AddressDto> GetOneAddressAsync(int addressId);
        Task<IEnumerable<AddressDto>> GetAllAddressesOfOneUserAsync(string userId);
        Task<OperationResult<AddressDto>> CreateAddressAsync(AddressDtoForCreation addressDto);
        Task<OperationResult<AddressDto>> MakeAddressDefaultAsync(int addressId);
        Task<OperationResult<AddressDto>> UpdateAddressAsync(AddressDtoForUpdate addressDto);
        Task<OperationResult<AddressDto>> DeleteAddressAsync(int addressId);
    }
}
