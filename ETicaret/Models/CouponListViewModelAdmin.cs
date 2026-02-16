using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class CouponListViewModelAdmin
    {
        public IEnumerable<CouponDto> Coupons { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public CouponRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
