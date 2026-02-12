using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICampaignService
    {
        Task<IEnumerable<CampaignDto>> GetAllAsync();
        Task<CampaignDto> GetByIdAsync(int campaignId);
        Task<IEnumerable<CampaignDto>> GetActiveAsync();
        Task<IEnumerable<Campaign>> GetApplicableAsync(decimal orderAmount);

        Task<OperationResult<int>> CreateAsync(CampaignDtoForCreation campaignDto);
        Task<OperationResult<CampaignDto>> UpdateAsync(CampaignDtoForUpdate campaignDto);
        Task<OperationResult<CampaignDto>> DeleteAsync(int campaignId);

        Task<OperationResult<CampaignDto>> ActivateAsync(int campaignId);
        Task<OperationResult<CampaignDto>> DeactivateAsync(int campaignId);
    }
}
