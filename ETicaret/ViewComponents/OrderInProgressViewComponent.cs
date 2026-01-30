/*using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class OrderInProgressViewComponent : ViewComponent
    {
        private readonly IOrderService _orderService;
        public OrderInProgressViewComponent(IOrderService orderService)
        {
            _orderService = orderService;
        }
        public async Task<string> InvokeAsync()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return orders.Count().ToString();
        }
    }
}*/
