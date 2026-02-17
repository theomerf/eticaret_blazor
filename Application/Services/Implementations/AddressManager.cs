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

        public async Task<IEnumerable<AddressDto>> GetAllAsync()
        {
            var addresses = await _manager.Address.GetAllAsync(false);
            var addressesDto = _mapper.Map<IEnumerable<AddressDto>>(addresses); 
            
            return addressesDto;
        }

        private async Task<Address> GetOneAddressForServiceAsync(int addressId, bool trackChanges)
        {
            var address = await _manager.Address.GetByIdAsync(addressId, trackChanges);

            if (address == null)
            {
                throw new AddressNotFoundException(addressId);
            }

            return address;
        }

        public async Task<AddressDto> GetByIdAsync(int addressId)
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

        public async Task<IEnumerable<AddressDto>> GetByUserIdAsync(string userId)
        {
            var addresses =  await _manager.Address.GetByUserIdAsync(userId, false);
            var addressesDto = _mapper.Map<IEnumerable<AddressDto>>(addresses);

            return addressesDto;
        }

        public async Task<OperationResult<AddressDto>> CreateAsync(AddressDtoForCreation addressDto)
        {
            try
            {
                _manager.ClearTracker();
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var address = _mapper.Map<Address>(addressDto);
                address.UserId = userId;

                address.ValidateForCreation();

                if (addressDto.IsDefault)
                {
                    await _manager.Address.UnsetDefaultForUserAsync(userId);
                }

                _manager.Address.Create(address);
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

        public async Task<OperationResult<AddressDto>> MakeDefaultAsync(int addressId)
        {
            try
            {
                _manager.ClearTracker();
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var address = await GetOneAddressForServiceAsync(addressId, true);

                if (address.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                if (address.IsDefault)
                {
                    return OperationResult<AddressDto>.Success("Adres zaten varsayılan olarak işaretli.");
                }

                address.ValidateForUpdate();

                await _manager.Address.UnsetDefaultForUserAsync(userId);

                address.IsDefault = true;
                address.UpdatedAt = DateTime.UtcNow;

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

        public async Task<OperationResult<AddressDto>> UpdateAsync(AddressDtoForUpdate addressDto)
        {
            try
            {
                _manager.ClearTracker();
                var address = await GetOneAddressForServiceAsync(addressDto.AddressId, true);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

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

                if (addressDto.IsDefault)
                {
                    await _manager.Address.UnsetDefaultForUserAsync(userId);
                }

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

        public async Task<OperationResult<AddressDto>> DeleteAsync(int addressId)
        {
            try
            {
                _manager.ClearTracker();
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var address = await GetOneAddressForServiceAsync(addressId, true);

                if (address.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                if (address.IsDefault)
                {
                    return OperationResult<AddressDto>.Failure(
                        "Varsayılan adres silinemez. Lütfen önce başka bir adresi varsayılan yapın.",
                        ResultType.ValidationError);
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
