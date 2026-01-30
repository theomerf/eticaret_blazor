namespace Domain.Exceptions
{
    public class CampaignValidationException : Exception
    {
        public CampaignValidationException(string message) : base(message)
        {
        }

        public CampaignValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
