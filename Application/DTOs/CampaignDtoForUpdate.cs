using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CampaignDtoForUpdate : CampaignDtoForCreation
    {
        [Required(ErrorMessage = "Kampanya ID gereklidir.")]
        public int CampaignId { get; set; }
    }
}
