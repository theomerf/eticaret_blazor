using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICampaignService
    {
        Task<IEnumerable<CampaignDto>> GetAllCampaignsAsync();
        Task<CampaignDto> GetCampaignByIdAsync(int campaignId);
        Task<IEnumerable<CampaignDto>> GetActiveCampaignsAsync();
        Task<IEnumerable<Campaign>> GetApplicableCampaignsAsync(decimal orderAmount);

        Task<OperationResult<int>> CreateCampaignAsync(CampaignDtoForCreation campaignDto);
        Task<OperationResult<CampaignDto>> UpdateCampaignAsync(CampaignDtoForUpdate campaignDto);
        Task<OperationResult<CampaignDto>> DeleteCampaignAsync(int campaignId);

        Task<OperationResult<CampaignDto>> ActivateCampaignAsync(int campaignId);
        Task<OperationResult<CampaignDto>> DeactivateCampaignAsync(int campaignId);
    }
}
