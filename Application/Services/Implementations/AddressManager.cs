using Application.Common.Exceptions;
using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Application.Services.Implementations
{
    public class AddressManager : IAddressService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AddressManager> _logger;
        private readonly ISecurityLogService _securityLogService;   

        public AddressManager(IRepositoryManager manager, IMapper mapper, IHttpContextAccessor httpContextAccessor, ILogger<AddressManager> logger, ISecurityLogService securityLogService)
        {
            _manager = manager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _securityLogService = securityLogService;
        }

        public async Task<IEnumerable<AddressDto>> GetAllAddressesAsync()
        {
            var addresses = await _manager.Address.GetAllAddressesAsync(false);
            var addressesDto = _mapper.Map<IEnumerable<AddressDto>>(addresses); 
            
            return addressesDto;
        }

        private async Task<Address> GetOneAddressForServiceAsync(int addressId, bool trackChanges)
        {
            var address = await _manager.Address.GetOneAddressAsync(addressId, trackChanges);

            if (address == null)
            {
                throw new AddressNotFoundException(addressId);
            }

            return address;
        }

        public async Task<AddressDto> GetOneAddressAsync(int addressId)
        {
            var address = await GetOneAddressForServiceAsync(addressId, false);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (address.UserId != userId)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }
            var addressDto = _mapper.Map<AddressDto>(address);

            return addressDto;
        }

        public async Task<IEnumerable<AddressDto>> GetAllAddressesOfOneUserAsync(string userId)
        {
            var addresses =  await _manager.Address.GetAllUserAddressesOfOneUserAsync(userId, false);
            var addressesDto = _mapper.Map<IEnumerable<AddressDto>>(addresses);

            return addressesDto;
        }

        public async Task<OperationResult<AddressDto>> CreateAddressAsync(AddressDtoForCreation addressDto)
        {
            try
            {
                _manager.ClearTracker();
                var address = _mapper.Map<Address>(addressDto);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                if (addressDto.IsDefault)
                {
                    var userAddresses = await _manager.Address.GetAllUserAddressesOfOneUserAsync(userId, true);
                    foreach (var addr in userAddresses)
                    {
                        if (addr.IsDefault)
                        {
                            addr.IsDefault = false;
                            addr.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                address.UserId = userId;

                address.ValidateForCreation();

                _manager.Address.CreateAddress(address);
                await _manager.SaveAsync();

                _logger.LogInformation("Address created successfully for user {UserId}", userId);

                return OperationResult<AddressDto>.Success("Adres başarıyla oluşturuldu.");
            }
            catch (AddressValidationException ex)
            {
                _logger.LogWarning("Address validation failed.");
                return OperationResult<AddressDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<AddressDto>> MakeAddressDefaultAsync(int addressId)
        {
            try
            {
                _manager.ClearTracker();
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var addresses = await _manager.Address.GetAllUserAddressesOfOneUserAsync(userId, true);
                var addressToMakeDefault = addresses.Where(a => a.AddressId == addressId).FirstOrDefault();

                if (addressToMakeDefault == null)
                {
                    throw new AddressNotFoundException(addressId);
                }

                if (addressToMakeDefault.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                foreach (var addr in addresses)
                {
                    if (addr.IsDefault && addr.AddressId != addressId)
                    {
                        addr.IsDefault = false;
                        addr.UpdatedAt = DateTime.UtcNow;
                    }
                }

                addressToMakeDefault.IsDefault = true;
                addressToMakeDefault.UpdatedAt = DateTime.UtcNow;

                addressToMakeDefault.ValidateForUpdate();

                await _manager.SaveAsync();

                _logger.LogInformation("Address status updated successfully for user {UserId}", userId);

                return OperationResult<AddressDto>.Success("Adres başarıyla varsayılan olarak işaretlendi.");
            }
            catch (AddressValidationException ex)
            {
                _logger.LogWarning("Address validation failed.");
                return OperationResult<AddressDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<AddressDto>> UpdateAddressAsync(AddressDtoForUpdate addressDto)
        {
            try
            {
                _manager.ClearTracker();
                var address = await GetOneAddressForServiceAsync(addressDto.AddressId, true);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                if (addressDto.IsDefault)
                {
                    var userAddresses = await _manager.Address.GetAllUserAddressesOfOneUserAsync(userId, true);
                    foreach (var addr in userAddresses)
                    {
                        if (addr.IsDefault && addr.AddressId != addressDto.AddressId)
                        {
                            addr.IsDefault = false;
                            addr.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                if (address.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                address.UpdatedAt = DateTime.UtcNow;
                _mapper.Map(addressDto, address);

                address.ValidateForUpdate();

                await _manager.SaveAsync();

                _logger.LogInformation("Address updated successfully for user {UserId}", userId);

                return OperationResult<AddressDto>.Success("Adres başarıyla güncellendi.");
            }
            catch (AddressValidationException ex)
            {
                _logger.LogWarning("Address validation failed.");
                return OperationResult<AddressDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<AddressDto>> DeleteAddressAsync(int addressId)
        {
            try
            {
                _manager.ClearTracker();
                var address = await GetOneAddressForServiceAsync(addressId, true);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                if (address.IsDefault)
                {
                    var userAddresses = await _manager.Address.GetAllUserAddressesOfOneUserAsync(userId, true);
                    var addressToMakeDefault = userAddresses.FirstOrDefault(a => a.AddressId != addressId);
                    if (addressToMakeDefault != null)
                    {
                        addressToMakeDefault.IsDefault = true;
                        addressToMakeDefault.UpdatedAt = DateTime.UtcNow;
                    }
                }

                if (address.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                address.SoftDelete(userId);

                await _manager.SaveAsync();

                _logger.LogInformation("Address deleted successfully. AddressId: {AddressId}, UserId: {UserId}", addressId, userId);

                return OperationResult<AddressDto>.Success("Adres başarıyla silindi.");
            }
            catch (AddressValidationException ex)
            {
                _logger.LogWarning("Address validation failed.");
                return OperationResult<AddressDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }
    }
}
