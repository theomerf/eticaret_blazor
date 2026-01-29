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

                var queryParamName = char.ToLowerInvariant(propName[0]) + propName.Substring(1);

                if (query.TryGetValue(queryParamName, out var value) && !string.IsNullOrEmpty(value))
                {
                    try
                    {
                        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        if (propType == typeof(string))
                        {
                            prop.SetValue(obj, value.ToString());
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
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

                    if (value is bool boolValue)
                    {
                        parameters[propName] = boolValue.ToString().ToLower();
                    }
                    else
                    {
                        if (propName == "pageSize" && (int)value == 6 || propName == "pageNumber" && (int)value == 1)
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
    }
}