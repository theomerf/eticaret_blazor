namespace Application.Queries.RequestParameters
{
    public record UserReviewRequestParametersAdmin : RequestParametersAdmin
    {
        public bool? IsApproved { get; set; }
        public bool? IsFeatured { get; set; }
        public string? SortBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
