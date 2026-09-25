using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Test.Unit.Compilation;

public class TagOptionsParserTests
{
    [Fact]
    public void ParsesUnquotedKeysAndStringValues()
    {
        var options = TagOptionsParser.Parse("loopOver: \"row\"");
        Assert.Equal("row", options["loopOver"]);
    }

    [Fact]
    public void ParsesSingleQuotedStrings()
    {
        var options = TagOptionsParser.Parse("a: 'it\\'s', b: 'x'");
        Assert.Equal("it's", options["a"]);
        Assert.Equal("x", options["b"]);
    }

    [Fact]
    public void ParsesQuotedKeys()
    {
        var options = TagOptionsParser.Parse("\"my key\": 1, 'other': 2");
        Assert.Equal(1L, options["my key"]);
        Assert.Equal(2L, options["other"]);
    }

    [Fact]
    public void ParsesNumbersBooleansAndNull()
    {
        var options = TagOptionsParser.Parse("i: 5, f: -1.5, t: true, n: null");
        Assert.Equal(5L, options["i"]);
        Assert.Equal(-1.5, options["f"]);
        Assert.Equal(true, options["t"]);
        Assert.Null(options["n"]);
    }

    [Fact]
    public void ParsesNestedObjectsAndArrays()
    {
        var options = TagOptionsParser.Parse("obj: { a: [1, 2, { b: 'c' }] }, arr: [true, false,]");
        var obj = Assert.IsType<Dictionary<string, object?>>(options["obj"]);
        var arr = Assert.IsType<List<object?>>(obj["a"]);
        Assert.Equal(3, arr.Count);
        Assert.Equal("c", Assert.IsType<Dictionary<string, object?>>(arr[2])["b"]);
        Assert.Equal(new object?[] { true, false }, Assert.IsType<List<object?>>(options["arr"]));
    }

    [Fact]
    public void AllowsTrailingCommasAndWhitespace()
    {
        var options = TagOptionsParser.Parse("  a : 1 ,  ");
        Assert.Equal(1L, Assert.Single(options).Value);
    }

    [Fact]
    public void EmptyOptions()
    {
        Assert.Empty(TagOptionsParser.Parse(""));
        Assert.Empty(TagOptionsParser.Parse("   "));
    }

    [Theory]
    [InlineData("myOpt 5")]
    [InlineData("a: ")]
    [InlineData("a: 'unterminated")]
    [InlineData("a: 1 b: 2")]
    [InlineData(": 1")]
    public void ThrowsFormatExceptionOnInvalidInput(string input)
    {
        Assert.Throws<FormatException>(() => TagOptionsParser.Parse(input));
    }
}
