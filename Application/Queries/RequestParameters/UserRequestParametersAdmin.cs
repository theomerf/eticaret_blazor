namespace Application.Queries.RequestParameters
{
    public record UserRequestParametersAdmin : RequestParametersAdmin
    {
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
    }
}
