using Domain.Entities;

namespace Application.Queries.RequestParameters
{
    public record CouponRequestParametersAdmin : RequestParametersAdmin
    {
        public bool? IsActive { get; set; }
        public CouponScope? Scope { get; set; }
        public CouponType? Type { get; set; }
        public string? SortBy { get; set; }
    }
}
