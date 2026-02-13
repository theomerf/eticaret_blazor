using Application.DTOs;
using Application.Queries.RequestParameters;
using ETicaret.Components.Shared;

namespace ETicaret.Models
{
    public class OrderListViewModelAdmin
    {
        public IEnumerable<OrderDto>? Orders { get; set; } = new List<OrderDto>();
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public OrderFilterParametersAdmin FilterParams { get; set; } = new();
    }
}
