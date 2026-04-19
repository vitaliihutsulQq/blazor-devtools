using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime;

public static class ComponentParameterFormatter
{
    public static string? Format(object? value)
    {
        try
        {
            return value switch
            {
                null => "null",
                string stringValue => stringValue,
                char charValue => charValue.ToString(),
                bool boolValue => boolValue ? "true" : "false",
                sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                    => Convert.ToString(value, CultureInfo.InvariantCulture),
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
                TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
                TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
                Guid guid => guid.ToString(),
                Uri uri => uri.ToString(),
                Enum enumValue => enumValue.ToString(),
                Type type => type.FullName ?? type.Name,
                RenderFragment => "<render-fragment>",
                MulticastDelegate => "<delegate>",
                _ when TryFormatCollection(value, out var collectionValue) => collectionValue,
                _ => $"<{value.GetType().FullName ?? value.GetType().Name}>"
            };
        }
        catch
        {
            return $"<{value?.GetType().FullName ?? "unknown"}>";
        }
    }

    private static bool TryFormatCollection(object value, out string? formatted)
    {
        formatted = null;

        if (value is not System.Collections.IEnumerable enumerable || value is string)
        {
            return false;
        }

        var type = value.GetType();
        var builder = new StringBuilder();
        builder.Append('<');
        builder.Append(type.FullName ?? type.Name);

        if (TryGetCount(value, out var count))
        {
            builder.Append($"> Count = {count}");
            formatted = builder.ToString();
            return true;
        }

        var sampleCount = 0;
        foreach (var item in enumerable)
        {
            sampleCount++;
            if (sampleCount >= 3)
            {
                break;
            }
        }

        builder.Append($"> Sampled = {sampleCount}");
        formatted = builder.ToString();
        return true;
    }

    private static bool TryGetCount(object value, out int count)
    {
        switch (value)
        {
            case Array array:
                count = array.Length;
                return true;
            case System.Collections.ICollection collection:
                count = collection.Count;
                return true;
            default:
                var countProperty = value.GetType().GetProperty("Count");
                if (countProperty?.PropertyType == typeof(int) && countProperty.GetIndexParameters().Length == 0)
                {
                    var propertyValue = countProperty.GetValue(value);
                    if (propertyValue is int propertyCount)
                    {
                        count = propertyCount;
                        return true;
                    }
                }

                count = 0;
                return false;
        }
    }
}
