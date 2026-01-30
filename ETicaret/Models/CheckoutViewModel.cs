using Application.DTOs;

namespace ETicaret.Models
{
    public class CheckoutViewModel
    {
        public CartDto Cart { get; set; } = new CartDto();
        public IEnumerable<AddressDto> Addresses { get; set; } = new List<AddressDto>();
        public IEnumerable<CampaignDto> ActiveCampaigns { get; set; } = new List<CampaignDto>();
    }
}
