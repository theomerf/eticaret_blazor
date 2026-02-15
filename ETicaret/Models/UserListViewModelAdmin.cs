using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class UserListViewModelAdmin
    {
        public IEnumerable<UserDto> Users { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public UserRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
