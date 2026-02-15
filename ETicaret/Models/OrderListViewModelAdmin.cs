using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class OrderListViewModelAdmin
    {
        public IEnumerable<OrderDto> Orders { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public OrderRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
