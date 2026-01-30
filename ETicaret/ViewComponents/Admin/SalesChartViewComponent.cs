/*using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents.Admin
{
    public class SalesChartViewComponent : ViewComponent
    {
        private readonly IOrderService _orderService;

        public SalesChartViewComponent(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var last30DaysData = await _orderService.GetLast30DaysSalesDataAsync();
            return View(last30DaysData);
        }
    }

}
*/