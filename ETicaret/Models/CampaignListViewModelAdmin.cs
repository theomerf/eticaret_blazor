using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class CampaignListViewModelAdmin
    {
        public IEnumerable<CampaignDto> Campaigns { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public CampaignRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
