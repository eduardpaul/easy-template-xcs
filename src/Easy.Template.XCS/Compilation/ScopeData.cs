namespace Easy.Template.XCS.Compilation;

public class PathPart
{
    public Tag? Tag { get; set; }
    public int? Number { get; set; }
}

public class ScopeDataArgs
{
    public List<PathPart> Path { get; set; }

    /**
    * The string representation of the path.
    */
    public List<string> StrPath { get; set; }
    public object Data { get; set; }
}

public delegate object ScopeDataResolver(ScopeDataArgs args);

public class ScopeData
{
    public static object DefaultResolver(ScopeDataArgs args)
    {
        return RecursiveObjectFindByPath(args.Data, args.StrPath.ToArray());
    }

    public ScopeDataResolver scopeDataResolver;
    public dynamic allData;

    private readonly List<PathPart> path = new List<PathPart>();
    private readonly List<string> strPath = new List<string>();

    public ScopeData(dynamic data)
    {
        allData = data;
    }

    public void PathPush(PathPart pathPart)
    {
        path.Add(pathPart);
        var strItem = pathPart.Number.HasValue ? pathPart.Number.Value.ToString() : pathPart.Tag?.Name;
        strPath.Add(strItem);
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

    public object GetScopeData()
    {
        var args = new ScopeDataArgs
        {
            Path = path,
            StrPath = strPath,
            Data = allData
        };
        if (scopeDataResolver != null)
        {
            return scopeDataResolver(args);
        }
        return DefaultResolver(args);
    }

    private static object RecursiveObjectFindByPath(object data, string[] path)
    {
        var currentPropertyName = path[0];

        if (data is Array)
        {
            var array = data as Array;
            var index = int.Parse(currentPropertyName);
            var value = array.GetValue(index);
            if (path.Length == 1)
            {
                return value;
            }
            else
            {
                return RecursiveObjectFindByPath(value, path.Skip(1).ToArray());
            }
        }
        else if (data is IDictionary<string, object>)
        {
            var props = data as IDictionary<string, object>;
            if (path.Length == 1)
            {
                return props[currentPropertyName];
            }
            else
            {
                return RecursiveObjectFindByPath(props[currentPropertyName], path.Skip(1).ToArray());
            }
        }
        else
        {
            var value = ReflectPropertyValue(data, currentPropertyName);
            if (path.Length == 1)
            {
                return value;
            }
            else
            {
                return RecursiveObjectFindByPath(value, path.Skip(1).ToArray());
            }
        }
    }
    
    private static object ReflectPropertyValue(object source, string property)
    {
        return source.GetType().GetProperty(property).GetValue(source, null);
    }
}
