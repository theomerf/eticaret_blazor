namespace Application.Services.Interfaces
{
    public interface IActivityQueueService
    {
        string EnqueueLog(string title, string description, string icon, string colorClass, string? link = null);
    }
}
