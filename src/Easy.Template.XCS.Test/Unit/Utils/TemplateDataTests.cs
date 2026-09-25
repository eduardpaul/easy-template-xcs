using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Easy.Template.XCS.Plugins.Image;
using Easy.Template.XCS.Plugins.Link;
using Easy.Template.XCS.Utils;

namespace Easy.Template.XCS.Test.Unit.Utils;

public class TemplateDataTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age;
        public Person? Friend { get; set; }
    }

    [Fact]
    public void TryGetByPath_AnonymousObject()
    {
        var data = new { a = new { b = new[] { new { c = "value" } } } };

        Assert.True(TemplateData.TryGetByPath(data, new[] { "a", "b", "0", "c" }, out var value));
        Assert.Equal("value", value);

        Assert.False(TemplateData.TryGetByPath(data, new[] { "a", "b", "1", "c" }, out _));
        Assert.False(TemplateData.TryGetByPath(data, new[] { "a", "x" }, out _));
    }

    [Fact]
    public void TryGetByPath_Poco_PropertiesAndFieldsCaseInsensitive()
    {
        var data = new Person { Name = "Alice", Age = 30, Friend = new Person { Name = "Bob" } };

        Assert.True(TemplateData.TryGetMember(data, "Name", out var name));
        Assert.Equal("Alice", name);
        Assert.True(TemplateData.TryGetMember(data, "name", out name));
        Assert.Equal("Alice", name);
        Assert.True(TemplateData.TryGetMember(data, "age", out var age));
        Assert.Equal(30, age);
        Assert.True(TemplateData.TryGetByPath(data, new[] { "friend", "name" }, out var friendName));
        Assert.Equal("Bob", friendName);
    }

    [Fact]
    public void TryGetByPath_Dictionaries()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, string> { ["b"] = "typed dict" },
            ["list"] = new List<object> { "x", "y" }
        };
        dynamic expando = new ExpandoObject();
        expando.hello = "world";
        data["expando"] = expando;

        Assert.True(TemplateData.TryGetByPath(data, new[] { "a", "b" }, out var value));
        Assert.Equal("typed dict", value);
        Assert.True(TemplateData.TryGetByPath(data, new[] { "list", "1" }, out value));
        Assert.Equal("y", value);
        Assert.True(TemplateData.TryGetByPath(data, new[] { "expando", "hello" }, out value));
        Assert.Equal("world", value);
        Assert.False(TemplateData.TryGetByPath(data, new[] { "list", "x" }, out _));
    }

    [Fact]
    public void TryGetByPath_Json()
    {
        const string json = """{ "a": { "b": [ { "c": "value", "n": 3, "f": 1.5, "t": true, "z": null } ] } }""";

        var node = JsonNode.Parse(json);
        Assert.True(TemplateData.TryGetByPath(node, new[] { "a", "b", "0", "c" }, out var value));
        Assert.Equal("value", value);
        Assert.True(TemplateData.TryGetByPath(node, new[] { "a", "b", "0", "n" }, out value));
        Assert.Equal(3L, value);
        Assert.True(TemplateData.TryGetByPath(node, new[] { "a", "b", "0", "z" }, out value));
        Assert.Null(value);

        var element = JsonDocument.Parse(json).RootElement;
        Assert.True(TemplateData.TryGetByPath(element, new[] { "a", "b", "0", "f" }, out value));
        Assert.Equal(1.5, value);
        Assert.True(TemplateData.TryGetByPath(element, new[] { "a", "b", "0", "t" }, out value));
        Assert.Equal(true, value);
        Assert.False(TemplateData.TryGetByPath(element, new[] { "a", "b", "5" }, out _));
    }

    [Fact]
    public void IsList()
    {
        Assert.True(TemplateData.IsList(new[] { 1, 2 }));
        Assert.True(TemplateData.IsList(new List<object>()));
        Assert.True(TemplateData.IsList(JsonNode.Parse("[1]")));
        Assert.True(TemplateData.IsList(JsonDocument.Parse("[1]").RootElement));
        Assert.False(TemplateData.IsList("string"));
        Assert.False(TemplateData.IsList(new byte[] { 1 }));
        Assert.False(TemplateData.IsList(new Dictionary<string, object?>()));
        Assert.False(TemplateData.IsList(JsonNode.Parse("{}")));
        Assert.False(TemplateData.IsList(new ImageContent()));
        Assert.False(TemplateData.IsList(null));
        Assert.False(TemplateData.IsList(5));
    }

    [Fact]
    public void ToList()
    {
        Assert.Equal(new object?[] { 1, 2 }, TemplateData.ToList(new[] { 1, 2 }));
        Assert.Equal(new object?[] { 1L, "a" }, TemplateData.ToList(JsonNode.Parse("[1, \"a\"]")));
        Assert.Equal(new object?[] { 1L, "a" }, TemplateData.ToList(JsonDocument.Parse("[1, \"a\"]").RootElement));
        Assert.Empty(TemplateData.ToList("not a list"));
    }

    [Fact]
    public void IsTruthy()
    {
        Assert.False(TemplateData.IsTruthy(null));
        Assert.False(TemplateData.IsTruthy(false));
        Assert.False(TemplateData.IsTruthy(0));
        Assert.False(TemplateData.IsTruthy(0.0));
        Assert.False(TemplateData.IsTruthy(""));
        Assert.False(TemplateData.IsTruthy(JsonNode.Parse("false")));
        Assert.False(TemplateData.IsTruthy(JsonDocument.Parse("0").RootElement));
        Assert.False(TemplateData.IsTruthy(JsonDocument.Parse("null").RootElement));

        Assert.True(TemplateData.IsTruthy(true));
        Assert.True(TemplateData.IsTruthy(1));
        Assert.True(TemplateData.IsTruthy(-1.5));
        Assert.True(TemplateData.IsTruthy("false"));
        Assert.True(TemplateData.IsTruthy(new object()));
        Assert.True(TemplateData.IsTruthy(Array.Empty<int>()));
        Assert.True(TemplateData.IsTruthy(JsonNode.Parse("\"x\"")));
        Assert.True(TemplateData.IsTruthy(JsonDocument.Parse("{}").RootElement));
    }

    [Fact]
    public void StringValue()
    {
        Assert.Equal("", TemplateData.StringValue(null));
        Assert.Equal("hello", TemplateData.StringValue("hello"));
        Assert.Equal("123", TemplateData.StringValue(123));
        Assert.Equal("1.5", TemplateData.StringValue(1.5));
        Assert.Equal("true", TemplateData.StringValue(true));
        Assert.Equal("false", TemplateData.StringValue(JsonNode.Parse("false")));
        Assert.Equal("2.5", TemplateData.StringValue(JsonDocument.Parse("2.5").RootElement));
        Assert.Equal("", TemplateData.StringValue(JsonDocument.Parse("null").RootElement));
        Assert.Equal("01/02/2024", TemplateData.StringValue(new DateOnly(2024, 1, 2)));
    }

    [Fact]
    public void StringValue_IsCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("1.5", TemplateData.StringValue(1.5));
            Assert.Equal("1.5", TemplateData.StringValue(1.5m));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetPluginContentType()
    {
        Assert.Equal("image", TemplateData.GetPluginContentType(new ImageContent()));
        Assert.Equal("link", TemplateData.GetPluginContentType(new Dictionary<string, object?> { ["_type"] = "link" }));
        Assert.Equal("rawXml", TemplateData.GetPluginContentType(JsonNode.Parse("""{ "_type": "rawXml" }""")));
        Assert.Equal("rawXml", TemplateData.GetPluginContentType(JsonDocument.Parse("""{ "_type": "rawXml" }""").RootElement));
        Assert.Null(TemplateData.GetPluginContentType(new Dictionary<string, object?> { ["_type"] = 5 }));
        Assert.Null(TemplateData.GetPluginContentType(new { _type = "not supported for anonymous objects" }));
        Assert.Null(TemplateData.GetPluginContentType("text"));
        Assert.Null(TemplateData.GetPluginContentType(null));
    }

    [Fact]
    public void ContentMapper_MapsDictionariesToContentClasses()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var dict = new Dictionary<string, object?>
        {
            ["_type"] = "image",
            ["source"] = bytes,
            ["format"] = "image/png",
            ["width"] = 100,
            ["height"] = "50",
            ["TransparencyPercent"] = 12.5
        };

        var content = ContentMapper.Map<ImageContent>(dict)!;
        Assert.Same(bytes, content.Source);
        Assert.Equal("image/png", content.Format);
        Assert.Equal(100, content.Width);
        Assert.Equal(50, content.Height);
        Assert.Equal(12.5, content.TransparencyPercent);
        Assert.Null(content.AltText);
    }

    [Fact]
    public void ContentMapper_MapsJsonAndAnonymousObjects()
    {
        var json = JsonNode.Parse("""{ "_type": "link", "target": "https://x", "text": "X" }""");
        var link = ContentMapper.Map<LinkContent>(json)!;
        Assert.Equal("https://x", link.Target);
        Assert.Equal("X", link.Text);
        Assert.Null(link.Tooltip);

        var anon = ContentMapper.Map<LinkContent>(new { Target = "https://y", tooltip = "tip" })!;
        Assert.Equal("https://y", anon.Target);
        Assert.Equal("tip", anon.Tooltip);

        var typed = new LinkContent("https://z");
        Assert.Same(typed, ContentMapper.Map<LinkContent>(typed));
        Assert.Null(ContentMapper.Map<LinkContent>(null));
    }

    [Fact]
    public void ContentMapper_ConvertsBase64AndStreamsToBytes()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var fromBase64 = ContentMapper.Map<ImageContent>(new { source = Convert.ToBase64String(bytes) })!;
        Assert.Equal(bytes, fromBase64.Source);

        using var stream = new MemoryStream(bytes);
        var fromStream = ContentMapper.Map<ImageContent>(new { source = stream })!;
        Assert.Equal(bytes, fromStream.Source);
    }
}
