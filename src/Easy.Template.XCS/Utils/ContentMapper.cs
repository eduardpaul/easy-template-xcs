using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Easy.Template.XCS.Utils;

/// <summary>
/// Maps loosely typed template data (dictionaries, json nodes, anonymous
/// objects) into strongly typed plugin content classes.
/// </summary>
public static class ContentMapper
{
    /// <summary>
    /// Convert the specified value into an instance of <typeparamref name="T"/>.
    /// Returns null if the value is null.
    /// </summary>
    public static T? Map<T>(object? value) where T : class, new()
    {
        value = TemplateData.Unwrap(value);
        if (value is null)
            return null;

        if (value is T typed)
            return typed;

        var result = new T();
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            if (!TryGetMemberIgnoreCase(value, property.Name, out var memberValue))
                continue;

            var converted = ConvertValue(memberValue, property.PropertyType);
            property.SetValue(result, converted);
        }

        return result;
    }

    private static bool TryGetMemberIgnoreCase(object value, string name, out object? memberValue)
    {
        if (TemplateData.TryGetMember(value, name, out memberValue))
            return true;

        // JS style camelCase keys
        var camelCase = char.ToLowerInvariant(name[0]) + name.Substring(1);
        if (camelCase != name && TemplateData.TryGetMember(value, camelCase, out memberValue))
            return true;

        // Any other casing
        foreach (var key in EnumerateKeys(value))
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase) && TemplateData.TryGetMember(value, key, out memberValue))
                return true;
        }

        memberValue = null;
        return false;
    }

    private static IEnumerable<string> EnumerateKeys(object value)
    {
        switch (value)
        {
            case IDictionary<string, object?> genericDict:
                return genericDict.Keys;
            case IReadOnlyDictionary<string, object?> readOnlyDict:
                return readOnlyDict.Keys;
            case IDictionary dict:
                return dict.Keys.OfType<string>();
            case JsonObject jsonObject:
                return jsonObject.Select(kv => kv.Key).ToList();
            case JsonElement { ValueKind: JsonValueKind.Object } element:
                return element.EnumerateObject().Select(p => p.Name).ToList();
            default:
                return Array.Empty<string>();
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        value = TemplateData.Unwrap(value);
        if (value is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
            return value;

        if (underlying == typeof(byte[]))
        {
            return value switch
            {
                string base64 => Convert.FromBase64String(base64),
                Stream stream => ReadAllBytes(stream),
                ReadOnlyMemory<byte> memory => memory.ToArray(),
                Memory<byte> memory => memory.ToArray(),
                IEnumerable<byte> bytes => bytes.ToArray(),
                _ => throw new InvalidCastException($"Cannot convert value of type '{value.GetType()}' to byte[].")
            };
        }

        if (underlying == typeof(string))
            return TemplateData.StringValue(value);

        if (underlying.IsEnum)
            return value is string enumName ? Enum.Parse(underlying, enumName, ignoreCase: true) : Enum.ToObject(underlying, value);

        if (underlying == typeof(bool))
            return value is string boolText ? bool.Parse(boolText) : TemplateData.IsTruthy(value);

        if (typeof(IConvertible).IsAssignableFrom(underlying))
            return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);

        if (typeof(IList<string>).IsAssignableFrom(underlying) || underlying == typeof(IEnumerable<string>) || underlying == typeof(IReadOnlyList<string>))
            return TemplateData.ToList(value).Select(TemplateData.StringValue).ToList();

        return value;
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
