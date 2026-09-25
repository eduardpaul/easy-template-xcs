using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Easy.Template.XCS.Plugins;

namespace Easy.Template.XCS.Utils;

/// <summary>
/// Helpers for reading template data.
///
/// Template data can be any of the following (and can be mixed freely):
/// - Dictionaries (<c>IDictionary&lt;string, T&gt;</c>, <c>ExpandoObject</c>, ...)
/// - Plain objects (anonymous types, records, POCOs) - properties are read via reflection
/// - <c>System.Text.Json</c> nodes (<c>JsonNode</c>, <c>JsonElement</c>)
/// - Lists and arrays (for loops)
/// - Primitives (string, numbers, booleans)
/// - <see cref="PluginContent"/> instances (or dictionaries with a "_type" key)
/// </summary>
public static class TemplateData
{
    public const string PluginContentTypeKey = "_type";

    /// <summary>
    /// Traverse the data object following the specified path.
    /// Returns false if any part of the path could not be resolved.
    /// </summary>
    public static bool TryGetByPath(object? data, IEnumerable<string> path, out object? value)
    {
        var current = data;
        foreach (var key in path)
        {
            if (!TryGetMember(current, key, out current))
            {
                value = null;
                return false;
            }
        }

        value = current;
        return true;
    }

    /// <summary>
    /// Read a single member (property, dictionary entry or list item) of the specified object.
    /// </summary>
    public static bool TryGetMember(object? obj, string key, out object? value)
    {
        value = null;
        obj = Unwrap(obj);
        if (obj is null)
            return false;

        switch (obj)
        {
            case string:
                return false;

            case IDictionary<string, object?> genericDict:
                if (!genericDict.TryGetValue(key, out var v1))
                    return false;
                value = Unwrap(v1);
                return true;

            case IReadOnlyDictionary<string, object?> readOnlyDict:
                if (!readOnlyDict.TryGetValue(key, out var v2))
                    return false;
                value = Unwrap(v2);
                return true;

            case IDictionary dict:
                if (!dict.Contains(key))
                    return false;
                value = Unwrap(dict[key]);
                return true;

            case JsonObject jsonObject:
                if (!jsonObject.TryGetPropertyValue(key, out var jsonNode))
                    return false;
                value = Unwrap(jsonNode);
                return true;

            case JsonElement { ValueKind: JsonValueKind.Object } jsonElement:
                if (!jsonElement.TryGetProperty(key, out var jsonProp))
                    return false;
                value = Unwrap(jsonProp);
                return true;

            case JsonElement { ValueKind: JsonValueKind.Array } jsonArray:
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var jsonIndex))
                    return false;
                if (jsonIndex < 0 || jsonIndex >= jsonArray.GetArrayLength())
                    return false;
                value = Unwrap(jsonArray[jsonIndex]);
                return true;

            case IList list:
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                    return false;
                if (index < 0 || index >= list.Count)
                    return false;
                value = Unwrap(list[index]);
                return true;

            case IEnumerable enumerable when int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumIndex):
                {
                    var i = 0;
                    foreach (var item in enumerable)
                    {
                        if (i++ == enumIndex)
                        {
                            value = Unwrap(item);
                            return true;
                        }
                    }
                    return false;
                }
        }

        return TryGetReflectedMember(obj, key, out value);
    }

    private static bool TryGetReflectedMember(object obj, string key, out object? value)
    {
        value = null;
        var type = obj.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        var property = type.GetProperty(key, flags)
            ?? type.GetProperty(key, flags | BindingFlags.IgnoreCase);
        if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
        {
            value = Unwrap(property.GetValue(obj));
            return true;
        }

        var field = type.GetField(key, flags)
            ?? type.GetField(key, flags | BindingFlags.IgnoreCase);
        if (field != null)
        {
            value = Unwrap(field.GetValue(obj));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unwrap <c>System.Text.Json</c> primitive wrappers into plain CLR values.
    /// </summary>
    public static object? Unwrap(object? value)
    {
        switch (value)
        {
            case JsonValue jsonValue:
                {
                    if (jsonValue.TryGetValue<string>(out var s)) return s;
                    if (jsonValue.TryGetValue<bool>(out var b)) return b;
                    if (jsonValue.TryGetValue<long>(out var l)) return l;
                    if (jsonValue.TryGetValue<double>(out var d)) return d;
                    if (jsonValue.TryGetValue<decimal>(out var m)) return m;
                    if (jsonValue.TryGetValue<JsonElement>(out var e)) return Unwrap(e);
                    return jsonValue.ToJsonString();
                }
            case JsonElement element:
                return element.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
                    _ => element
                };
            default:
                return value;
        }
    }

    /// <summary>
    /// Is the specified value a list (i.e. should be treated as loop data).
    /// </summary>
    public static bool IsList(object? value)
    {
        value = Unwrap(value);
        return value switch
        {
            null => false,
            string => false,
            byte[] => false,
            PluginContent => false,
            IDictionary => false,
            JsonObject => false,
            JsonArray => true,
            JsonElement e => e.ValueKind == JsonValueKind.Array,
            IEnumerable => true,
            _ => false
        };
    }

    /// <summary>
    /// Convert a list value into a list of items. Returns an empty list for non-list values.
    /// </summary>
    public static IReadOnlyList<object?> ToList(object? value)
    {
        value = Unwrap(value);
        if (!IsList(value))
            return Array.Empty<object?>();

        if (value is JsonElement e)
            return e.EnumerateArray().Select(item => Unwrap(item)).ToList();

        return ((IEnumerable)value!).Cast<object?>().Select(Unwrap).ToList();
    }

    /// <summary>
    /// JavaScript-like truthiness check, used to evaluate conditions.
    /// </summary>
    public static bool IsTruthy(object? value)
    {
        value = Unwrap(value);
        switch (value)
        {
            case null:
                return false;
            case bool b:
                return b;
            case string s:
                return s.Length > 0;
            case JsonElement e:
                return e.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined or JsonValueKind.False => false,
                    JsonValueKind.Number => e.GetDouble() != 0,
                    JsonValueKind.String => (e.GetString() ?? "").Length > 0,
                    _ => true
                };
        }

        if (IsNumeric(value))
        {
            var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return d != 0 && !double.IsNaN(d);
        }

        return true;
    }

    /// <summary>
    /// Convert a value to its string representation (culture invariant).
    /// Null values are converted to an empty string.
    /// </summary>
    public static string StringValue(object? value)
    {
        value = Unwrap(value);
        return value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            JsonElement e => e.ValueKind == JsonValueKind.Null ? string.Empty : e.ToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    /// <summary>
    /// If the value is a plugin content (i.e. it explicitly specifies its
    /// content type) returns the content type. Otherwise returns null.
    /// </summary>
    public static string? GetPluginContentType(object? value)
    {
        value = Unwrap(value);
        switch (value)
        {
            case null:
                return null;
            case PluginContent pluginContent:
                return pluginContent.ContentType;
            case string:
                return null;
        }

        if (value is IDictionary or IDictionary<string, object?> or IReadOnlyDictionary<string, object?> or JsonObject or JsonElement { ValueKind: JsonValueKind.Object })
        {
            if (TryGetMember(value, PluginContentTypeKey, out var typeValue) && typeValue is string typeName && typeName.Length > 0)
                return typeName;
        }

        return null;
    }

    public static bool IsNumeric(object? value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}
