using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> source,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            return condition
                ? source.Where(predicate)
                : source;
        }

        public static IQueryable<T> IncludeIf<T, TProperty>(
            this IQueryable<T> source,
            bool condition,
            Expression<Func<T, TProperty>> path)
            where T : class
        {
            return condition ? source.Include(path) : source;
        }

    }
}
