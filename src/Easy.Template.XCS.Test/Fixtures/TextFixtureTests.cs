using System.Xml.Linq;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class TextFixtureTests
{
    [Fact]
    public async Task ReplacesASingleTag()
    {
        var handler = new TemplateHandler();

        // load the template
        var template = ReadFixture("simple.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{simple_prop}", templateText.Trim());

        // replace tags
        var data = new { simple_prop = "hello world" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello world", docText.Trim());
    }

    [Fact]
    public async Task ReplacesASingleTag_DictionaryData()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");

        var data = new Dictionary<string, object?> { ["simple_prop"] = "hello world" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello world", docText.Trim());
    }

    [Fact]
    public async Task ReplacesASingleTag_JsonData()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");

        var data = System.Text.Json.Nodes.JsonNode.Parse("""{ "simple_prop": "hello json" }""");
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello json", docText.Trim());

        var element = System.Text.Json.JsonDocument.Parse("""{ "simple_prop": "hello element" }""").RootElement;
        doc = await handler.ProcessAsync(template, element);
        docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello element", docText.Trim());
    }

    [Fact]
    public async Task RemovesEmptyTags()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("empty tag.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{}{ }", templateText.Trim());

        var data = new { };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("", docText.Trim());
    }

    [Fact]
    public async Task SkipsEmptyTagsIfSkipEmptyTagsIsTrue()
    {
        var handler = new TemplateHandler(new TemplateHandlerOptions
        {
            SkipEmptyTags = true
        });

        var template = ReadFixture("simple.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{simple_prop}", templateText.Trim());

        var data = new { simple_prop = "" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("{simple_prop}", docText.Trim());
    }

    [Fact]
    public async Task HandlesNumericValues()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = 123 };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("123", docText.Trim());
    }

    [Fact]
    public async Task HandlesNullValues()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = (string?)null };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("", docText.Trim());
    }

    [Fact]
    public async Task ReplacesNewlinesWithLineBreaks()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = "first line\nsecond line" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first linesecond line", docText.Trim());

        var docXml = await handler.GetXmlAsync(doc);
        Assert.NotNull(docXml);
        var run = docXml.Descendants(W + "r").Single(r => r.Elements(W + "t").Any());
        var runChildren = run.Elements().Where(e => e.Name != W + "rPr").Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "t", "br", "t" }, runChildren);
        Assert.Equal(new[] { "first line", "second line" }, run.Elements(W + "t").Select(t => t.Value));
    }

    [Fact]
    public async Task PreservesLeadingNewlinesInTextReplacement()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = "\nleading newline" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("leading newline", docText.Trim());

        var docXml = await handler.GetXmlAsync(doc);
        Assert.NotNull(docXml);
        var run = docXml.Descendants(W + "r").Single(r => r.Elements(W + "t").Any());
        var runChildren = run.Elements().Where(e => e.Name != W + "rPr").Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[] { "br", "t" }, runChildren);
    }

    [Fact]
    public async Task EscapesXmlSpecialCharacters()
    {
        var handler = new TemplateHandler();

        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = "i'm special </w:r>" };
        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("i'm special </w:r>", docText.Trim());
    }

    [Fact]
    public async Task ReplacesTagsInAttributes()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - placeholder - two tags.docx");

        var templateXml = await handler.GetXmlAsync(template);
        var altTextBefore = templateXml!.Descendants(Wp + "docPr").First().Attribute("descr")?.Value;
        Assert.Equal("Hello {Placeholder 1} {Placeholder 2}", altTextBefore);

        var data = new Dictionary<string, object?> { ["Placeholder 1"] = "World" };
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var altTextAfter = docXml!.Descendants(Wp + "docPr").First().Attribute("descr")?.Value;
        Assert.Equal("Hello World ", altTextAfter);
    }

    [Fact]
    public async Task RemovesAttributesIfTheReplacementIsEmpty()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - placeholder.docx");

        var templateXml = await handler.GetXmlAsync(template);
        var altTextBefore = templateXml!.Descendants(Wp + "docPr").First().Attribute("descr")?.Value;
        Assert.Equal("{My Tag 2}", altTextBefore);

        var data = new { };
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var altTextAfter = docXml!.Descendants(Wp + "docPr").First().Attribute("descr");
        Assert.Null(altTextAfter);
    }

    [Fact]
    public async Task StreamOverloadDoesNotDisposeOrConsumeTheInputStream()
    {
        var handler = new TemplateHandler();
        using var template = new MemoryStream(ReadFixture("simple.docx"));

        using var doc = await handler.ProcessAsync(template, new { simple_prop = "hello" });
        Assert.True(template.CanRead);

        template.Position = 0;
        using var doc2 = await handler.ProcessAsync(template, new { simple_prop = "world" });

        Assert.Equal("hello", (await handler.GetTextAsync(doc)).Trim());
        Assert.Equal("world", (await handler.GetTextAsync(doc2)).Trim());
    }
}
