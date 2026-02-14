using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace ETicaret.Binders
{
    public class DateOnlyModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            string[] formats = { "yyyy-MM-dd", "dd.MM.yyyy", "yyyy.MM.dd", "MM/dd/yyyy", "dd/MM/yyyy" };

            if (DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var cultureResult))
            {
                bindingContext.Result = ModelBindingResult.Success(cultureResult);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Geçersiz tarih formatı.");
            
            return Task.CompletedTask;
        }
    }

    public class DateOnlyModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(DateOnly) || context.Metadata.ModelType == typeof(DateOnly?))
            {
                return new DateOnlyModelBinder();
            }

            return null;
        }
    }
}
