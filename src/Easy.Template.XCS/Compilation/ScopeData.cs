using System.Globalization;
using Easy.Template.XCS.Utils;

namespace Easy.Template.XCS.Compilation;

/// <summary>
/// A single part of the data path - either a tag or a loop index.
/// </summary>
public sealed class PathPart
{
    public Tag? Tag { get; }

    public int? Index { get; }

    public bool IsIndex => Index.HasValue;

    private PathPart(Tag? tag, int? index)
    {
        Tag = tag;
        Index = index;
    }

    public static PathPart FromTag(Tag tag) => new(tag, null);

    public static PathPart FromIndex(int index) => new(null, index);

    public static implicit operator PathPart(Tag tag) => FromTag(tag);

    public static implicit operator PathPart(int index) => FromIndex(index);

    public override string ToString() => Index.HasValue ? Index.Value.ToString(CultureInfo.InvariantCulture) : Tag!.Name;
}

public sealed class ScopeDataArgs
{
    public required IReadOnlyList<PathPart> Path { get; init; }

    /// <summary>
    /// The string representation of the path.
    /// </summary>
    public required IReadOnlyList<string> StrPath { get; init; }

    public object? Data { get; init; }
}

public delegate object? ScopeDataResolver(ScopeDataArgs args);

public class ScopeData
{
    /// <summary>
    /// The default scope data resolver: looks for the last path part in the
    /// current scope, then in the parent scope and so on up to the root data
    /// object.
    /// </summary>
    public static object? DefaultResolver(ScopeDataArgs args)
    {
        if (args.StrPath.Count == 0)
            return args.Data;

        var lastKey = args.StrPath[args.StrPath.Count - 1];
        var curPath = new List<string>(args.StrPath);
        while (curPath.Count > 0)
        {
            curPath.RemoveAt(curPath.Count - 1);
            if (TemplateData.TryGetByPath(args.Data, curPath.Append(lastKey), out var result))
                return result;
        }
        return null;
    }

    public ScopeDataResolver? Resolver { get; set; }

    public object? AllData { get; }

    private readonly List<PathPart> path = new();
    private readonly List<string> strPath = new();

    public ScopeData(object? data)
    {
        AllData = data;
    }

    public void PathPush(PathPart pathPart)
    {
        path.Add(pathPart);
        strPath.Add(pathPart.ToString());
    }

    public PathPart PathPop()
    {
        strPath.RemoveAt(strPath.Count - 1);
        var pathPart = path[path.Count - 1];
        path.RemoveAt(path.Count - 1);
        return pathPart;
    }

    public string PathString()
    {
        return string.Join(".", strPath);
    }

    /// <summary>
    /// Get the data of the current scope.
    /// </summary>
    public object? GetScopeData()
    {
        var args = new ScopeDataArgs
        {
            Path = path,
            StrPath = strPath,
            Data = AllData
        };

        var resolver = Resolver ?? DefaultResolver;
        return TemplateData.Unwrap(resolver(args));
    }

    /// <summary>
    /// Get the data of the current scope, converted to the specified content type.
    /// </summary>
    public T? GetScopeData<T>() where T : class, new()
    {
        return ContentMapper.Map<T>(GetScopeData());
    }
}
