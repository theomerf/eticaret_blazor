namespace Application.Services.Interfaces
{
    public interface IDatabaseHealthService
    {
        Task<long?> CheckAsync(CancellationToken ct = default);
    }
}
