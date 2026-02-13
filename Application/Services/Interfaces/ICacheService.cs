namespace Application.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

        Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken ct = default);

        Task SetAsync<T>(string key, T value,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken ct = default);

        Task RemoveAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Prefix ile başlayan tüm cache key'lerini temizler.
        /// Örn: "products:" → tüm ürün cache'lerini temizler.
        /// </summary>
        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    }
}
