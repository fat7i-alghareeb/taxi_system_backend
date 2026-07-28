using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Taxi.Api.Infrastructure;

/// <summary>
/// Parses decimal/decimal? form and query values using invariant culture.
/// The framework default binds decimal via the ambient request culture, which
/// RequestLocalizationMiddleware sets from Accept-Language; under a culture
/// where '.' is a group separator (e.g. nl, de) an invariant-formatted value
/// like "51.9225" silently mis-parses instead of failing, so every client —
/// which always sends invariant-formatted numbers — must be bound this way.
/// </summary>
public sealed class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        try
        {
            var model = decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            bindingContext.ModelState.TryAddModelError(modelName, exception, bindingContext.ModelMetadata);
        }

        return Task.CompletedTask;
    }
}

public sealed class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.UnderlyingOrModelType == typeof(decimal)
            ? new InvariantDecimalModelBinder()
            : null;
    }
}
