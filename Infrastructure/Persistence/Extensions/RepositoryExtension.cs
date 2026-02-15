using System.Linq.Expressions;

namespace Infrastructure.Persistence.Extensions
{
    public enum FilterOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith
    }

    public static class RepositoryExtension
    {
        public static IQueryable<T> ToPaginate<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            return source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        public static IQueryable<T> FilterBy<T, TProperty>(
         this IQueryable<T> query,
         TProperty? value,
         Expression<Func<T, TProperty>> propertySelector,
         FilterOperator op = FilterOperator.Equal)
        {
            if (value == null)
                return query;
            if (value is string s && string.IsNullOrWhiteSpace(s))
                return query;

            var parameter = propertySelector.Parameters[0];
            var member = propertySelector.Body;
            var constant = Expression.Constant(value, typeof(TProperty));

            Expression body = op switch
            {
                FilterOperator.Equal => Expression.Equal(member, constant),
                FilterOperator.NotEqual => Expression.NotEqual(member, constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(member, constant),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, constant),
                FilterOperator.LessThan => Expression.LessThan(member, constant),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, constant),
                FilterOperator.Contains => Expression.Call(
                    member,
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    Expression.Constant(value.ToString()!, typeof(string))
                ),
                FilterOperator.StartsWith => Expression.Call(
                    member,
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                    Expression.Constant(value.ToString()!, typeof(string))
                ),
                FilterOperator.EndsWith => Expression.Call(
                    member,
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
                    Expression.Constant(value.ToString()!, typeof(string))
                ),
                _ => throw new NotImplementedException()
            };

            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

            return query.Where(lambda);
        }
    }
}
