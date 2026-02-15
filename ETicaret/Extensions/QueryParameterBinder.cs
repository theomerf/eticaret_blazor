using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace ETicaret.Extensions
{
    public static class QueryParameterBinder
    {
        public static T BindFromQuery<T>(this NavigationManager navigationManager) where T : new()
        {
            var uri = new Uri(navigationManager.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);
            var obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var propName = prop.Name;
                // Query string genellikle camelCase gelir
                var queryParamName = char.ToLowerInvariant(propName[0]) + propName.Substring(1);

                if (query.TryGetValue(queryParamName, out var value) && !string.IsNullOrEmpty(value) && prop.CanWrite)
                {
                    try
                    {
                        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        if (propType == typeof(string))
                        {
                            prop.SetValue(obj, value.ToString());
                        }
                        else if (propType.IsEnum)
                        {
                            if (Enum.TryParse(propType, value.ToString(), true, out var enumValue))
                                prop.SetValue(obj, enumValue);
                        }
                        else if (propType == typeof(int))
                        {
                            if (int.TryParse(value, out var intValue))
                                prop.SetValue(obj, intValue);
                        }
                        else if (propType == typeof(decimal))
                        {
                            if (decimal.TryParse(value, out var decimalValue))
                                prop.SetValue(obj, decimalValue);
                        }
                        else if (propType == typeof(bool))
                        {
                            if (bool.TryParse(value, out var boolValue))
                                prop.SetValue(obj, boolValue);
                        }
                        else if (propType == typeof(DateTime))
                        {
                            if (DateTime.TryParse(value, out var dateValue))
                                prop.SetValue(obj, dateValue);
                        }
                    }
                    catch
                    {
                        // Parsing hatası - devam et
                    }
                }
            }

            return obj;
        }

        public static string ToQueryString<T>(this T obj, string baseUrl)
        {
            var parameters = new Dictionary<string, string?>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;

                var value = prop.GetValue(obj);
                if (value != null)
                {
                    var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

                    if (value is bool boolValue)
                    {
                        parameters[propName] = boolValue.ToString().ToLower();
                    }
                    else if (value is Enum enumValue)
                    {
                        parameters[propName] = ToCamelCase(enumValue.ToString());
                    }
                    else
                    {
                        if (propName == "sortBy" && string.Equals(value.ToString(), "date_desc", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        if (value is DateTime dateValue)
                        {
                            parameters[propName] = dateValue.ToString("yyyy-MM-dd");
                            continue;
                        }
                        if (propName == "pageSize" && (int)value == 10 || propName == "pageNumber" && (int)value == 1)
                        {
                            continue;
                        }
                        parameters[propName] = value.ToString();
                    }
                }
            }

            var cleanParams = parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .ToDictionary(p => p.Key, p => p.Value!);

            return QueryHelpers.AddQueryString(baseUrl, cleanParams!);
        }

        private static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.Length == 1)
                return value.ToLowerInvariant();

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }
}
