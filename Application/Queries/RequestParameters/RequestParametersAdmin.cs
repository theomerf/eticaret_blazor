namespace Application.Queries.RequestParameters
{
    public class RequestParametersAdmin : RequestParameters
    {
        const int _maxPageSize = 100;
        private int _pageSize = 10;
        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > _maxPageSize) ? _maxPageSize : value; }
        }

        public virtual void Validate()
        {
            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize < 1)
                PageSize = 10;
        }
    }
}
