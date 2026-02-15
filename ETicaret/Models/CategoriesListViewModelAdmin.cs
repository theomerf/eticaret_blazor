using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class CategoriesListViewModelAdmin
    {

        public IEnumerable<CategoryDto> Categories { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public RequestParametersAdmin FilterParams { get; set; } = new();
    }
}
