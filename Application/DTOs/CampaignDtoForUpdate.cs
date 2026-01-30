using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for updating campaigns
    /// </summary>
    public record CampaignDtoForUpdate : CampaignDtoForCreation
    {
        [Required(ErrorMessage = "Kampanya ID gereklidir.")]
        public int CampaignId { get; set; }
    }
}
