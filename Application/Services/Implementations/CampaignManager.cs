using Application.Common.Exceptions;
using Application.Common.Models;
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
    public class CampaignManager : ICampaignService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ILogger<CampaignManager> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly IActivityService _activityService;

        public CampaignManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<CampaignManager> logger,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            IActivityService activityService)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _activityService = activityService;
        }

        private string GetCurrentUserId() =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string GetCurrentUserName() =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        public async Task<IEnumerable<CampaignDto>> GetAllAsync()
        {
            var campaigns = await _manager.Campaign.GetAllAsync(false);
            var campaignsDto = _mapper.Map<IEnumerable<CampaignDto>>(campaigns);

            return campaignsDto;
        }

        public async Task<CampaignDto> GetByIdAsync(int campaignId)
        {
            var campaign = await _manager.Campaign.GetByIdAsync(campaignId, false);
            if (campaign == null)
            {
                throw new CampaignNotFoundException(campaignId);
            }

            var campaignDto = _mapper.Map<CampaignDto>(campaign);

            return campaignDto;
        }

        public async Task<IEnumerable<CampaignDto>> GetActiveAsync()
        {
            var campaigns = await _manager.Campaign.GetActiveAsync(false);
            var campaignsDto = _mapper.Map<IEnumerable<CampaignDto>>(campaigns);

            return campaignsDto;
        }

        public async Task<IEnumerable<Campaign>> GetApplicableAsync(decimal orderAmount)
        {
            var activeCampaigns = await _manager.Campaign.GetActiveByPriorityAsync(false);

            var applicableCampaigns = activeCampaigns
                .Where(c => c.IsActiveNow() && (!c.MinOrderAmount.HasValue || orderAmount >= c.MinOrderAmount.Value))
                .ToList();

            return applicableCampaigns;
        }

        public async Task<OperationResult<int>> CreateAsync(CampaignDtoForCreation campaignDto)
        {
            try
            {
                var campaign = _mapper.Map<Campaign>(campaignDto);

                campaign.ValidateForCreation();

                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                campaign.CreatedByUserId = userId;
                campaign.UpdatedByUserId = userId;

                _manager.Campaign.Create(campaign);
                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "Campaign",
                    entityId: campaign.CampaignId.ToString(),
                    newValues: new
                    {
                        campaign.Name,
                        campaign.Type,
                        campaign.Value,
                        campaign.Scope,
                        campaign.StartsAt,
                        campaign.EndsAt,
                        campaign.Priority,
                        campaign.IsStackable
                    }
                );

                await _activityService.LogAsync(
                    "Yeni Kampanya",
                    $"{campaign.Name} kampanyası oluşturuldu.",
                    "fa-bullhorn",
                    "text-pink-500 bg-pink-100",
                    $"/admin/campaigns/edit/{campaign.CampaignId}"
                );

                _logger.LogInformation(
                    "Campaign created successfully. CampaignId: {CampaignId}, Name: {Name}, User: {UserId}",
                    campaign.CampaignId, campaign.Name, userId);

                return OperationResult<int>.Success(campaign.CampaignId, "Kampanya başarıyla oluşturuldu.");
            }
            catch (CampaignValidationException ex)
            {
                _logger.LogWarning(ex, "Campaign validation failed. Name: {Name}", campaignDto.Name);
                return OperationResult<int>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CampaignDto>> UpdateAsync(CampaignDtoForUpdate campaignDto)
        {
            try
            {
                _manager.ClearTracker();
                var campaign = await _manager.Campaign.GetByIdAsync(campaignDto.CampaignId, true);
                if (campaign == null)
                {
                    return OperationResult<CampaignDto>.Failure("Kampanya bulunamadı.", ResultType.NotFound);
                }

                var oldValues = new
                {
                    campaign.Name,
                    campaign.Type,
                    campaign.Value,
                    campaign.Scope,
                    campaign.StartsAt,
                    campaign.EndsAt,
                    campaign.Priority,
                    campaign.IsStackable,
                    campaign.IsActive
                };

                _mapper.Map(campaignDto, campaign);

                campaign.ValidateForCreation();

                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                campaign.UpdatedAt = DateTime.UtcNow;
                campaign.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "Campaign",
                    entityId: campaign.CampaignId.ToString(),
                    oldValues: oldValues,
                    newValues: new
                    {
                        campaign.Name,
                        campaign.Type,
                        campaign.Value,
                        campaign.Scope,
                        campaign.StartsAt,
                        campaign.EndsAt,
                        campaign.Priority,
                        campaign.IsStackable,
                        campaign.IsActive
                    }
                );

                _logger.LogInformation(
                    "Campaign updated successfully. CampaignId: {CampaignId}, User: {UserId}",
                    campaign.CampaignId, userId);

                var updatedCampaignDto = _mapper.Map<CampaignDto>(campaign);
                return OperationResult<CampaignDto>.Success(updatedCampaignDto, "Kampanya başarıyla güncellendi.");
            }
            catch (CampaignValidationException ex)
            {
                _logger.LogWarning(ex, "Campaign validation failed during update. CampaignId: {CampaignId}", campaignDto.CampaignId);
                return OperationResult<CampaignDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CampaignDto>> DeleteAsync(int campaignId)
        {
            var campaign = await _manager.Campaign.GetByIdAsync(campaignId, true);
            if (campaign == null)
            {
                return OperationResult<CampaignDto>.Failure("Kampanya bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            campaign.SoftDelete(userId);
            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "Campaign",
                entityId: campaignId.ToString()
            );

            _logger.LogInformation(
                "Campaign soft deleted. CampaignId: {CampaignId}, User: {UserId}",
                campaignId, userId);

            return OperationResult<CampaignDto>.Success("Kampanya başarıyla silindi.");
        }

        public async Task<OperationResult<CampaignDto>> ActivateAsync(int campaignId)
        {
            _manager.ClearTracker();
            var campaign = await _manager.Campaign.GetByIdAsync(campaignId, true);
            if (campaign == null)
            {
                return OperationResult<CampaignDto>.Failure("Kampanya bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            campaign.Activate();
            campaign.UpdatedAt = DateTime.UtcNow;
            campaign.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Activate",
                entityName: "Campaign",
                entityId: campaignId.ToString(),
                newValues: new { IsActive = true }
            );

            _logger.LogInformation(
                "Campaign activated. CampaignId: {CampaignId}, User: {UserId}",
                campaignId, userId);

            var campaignDto = _mapper.Map<CampaignDto>(campaign);
            return OperationResult<CampaignDto>.Success(campaignDto, "Kampanya aktif edildi.");
        }

        public async Task<OperationResult<CampaignDto>> DeactivateAsync(int campaignId)
        {
            _manager.ClearTracker();
            var campaign = await _manager.Campaign.GetByIdAsync(campaignId, true);
            if (campaign == null)
            {
                return OperationResult<CampaignDto>.Failure("Kampanya bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            campaign.Deactivate();
            campaign.UpdatedAt = DateTime.UtcNow;
            campaign.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Deactivate",
                entityName: "Campaign",
                entityId: campaignId.ToString(),
                newValues: new { IsActive = false }
            );

            _logger.LogInformation(
                "Campaign deactivated. CampaignId: {CampaignId}, User: {UserId}",
                campaignId, userId);

            var campaignDto = _mapper.Map<CampaignDto>(campaign);
            return OperationResult<CampaignDto>.Success(campaignDto, "Kampanya deaktif edildi.");
        }


    }
}
