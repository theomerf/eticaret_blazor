using Domain.Entities;

namespace Application.Queries.RequestParameters
{
    public record CampaignRequestParametersAdmin : RequestParametersAdmin
    {
        public bool? IsActive { get; set; }
        public CampaignScope? Scope { get; set; }
        public CampaignType? Type { get; set; }
        public string? SortBy { get; set; }
    }
}
