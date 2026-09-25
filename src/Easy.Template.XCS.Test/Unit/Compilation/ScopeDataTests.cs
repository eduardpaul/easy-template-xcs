using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Test.Unit.Compilation;

public class ScopeDataTests
{
    private static TextNodeTag TagNamed(string name) => new() { Name = name, XmlTextNode = new Text() };

    [Fact]
    public void UsesCustomScopeDataResolver_NoPath()
    {
        var scopeData = new ScopeData(new { });
        var value1 = scopeData.GetScopeData();
        Assert.NotNull(value1);

        scopeData.Resolver = args =>
        {
            Assert.Empty(args.Path);
            Assert.Empty(args.StrPath);
            return 7;
        };
        var value2 = scopeData.GetScopeData();
        Assert.Equal(7, value2);
    }

    [Fact]
    public void UsesCustomScopeDataResolver_WithPath()
    {
        var scopeData = new ScopeData(new { hello = "world" });
        scopeData.PathPush(TagNamed("hello"));
        var value1 = scopeData.GetScopeData();
        Assert.Equal("world", value1);

        scopeData.Resolver = args =>
        {
            Assert.Equal("hello", Assert.Single(args.Path).Tag?.Name);
            Assert.Equal(new[] { "hello" }, args.StrPath);
            return "Bobby";
        };
        var value2 = scopeData.GetScopeData();
        Assert.Equal("Bobby", value2);
    }

    [Fact]
    public void DefaultResolver_LooksUpTheScopeChain()
    {
        var data = new
        {
            outer = "outer value",
            list = new[]
            {
                new { inner = "inner 0" },
                new { inner = "inner 1" }
            }
        };
        var scopeData = new ScopeData(data);

        scopeData.PathPush(TagNamed("list"));
        scopeData.PathPush(1);
        Assert.Equal("list.1", scopeData.PathString());

        scopeData.PathPush(TagNamed("inner"));
        Assert.Equal("inner 1", scopeData.GetScopeData());
        scopeData.PathPop();

        // not found in the current scope - found in the root scope
        scopeData.PathPush(TagNamed("outer"));
        Assert.Equal("outer value", scopeData.GetScopeData());
        scopeData.PathPop();

        // not found anywhere
        scopeData.PathPush(TagNamed("missing"));
        Assert.Null(scopeData.GetScopeData());
        scopeData.PathPop();

        Assert.True(scopeData.PathPop().IsIndex);
        Assert.Equal("list", scopeData.PathPop().Tag?.Name);
        Assert.Equal("", scopeData.PathString());
    }

    [Fact]
    public void DefaultResolver_NullValueStopsTheSearch()
    {
        var data = new Dictionary<string, object?>
        {
            ["prop"] = "root",
            ["list"] = new[] { new Dictionary<string, object?> { ["prop"] = null } }
        };
        var scopeData = new ScopeData(data);
        scopeData.PathPush(TagNamed("list"));
        scopeData.PathPush(0);
        scopeData.PathPush(TagNamed("prop"));

        Assert.Null(scopeData.GetScopeData());
    }
}
