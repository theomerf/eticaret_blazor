namespace Application.Queries.RequestParameters
{
    public abstract record RequestParameters
    {
        public string? SearchTerm { get; set; }
    }
}
