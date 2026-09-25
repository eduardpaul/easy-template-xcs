using Easy.Template.XCS.Plugins.RawXml;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class RawXmlFixtureTests
{
    [Fact]
    public async Task InsertsRawXmlContentIntoTextNode()
    {
        var template = ReadFixture("simple.docx");
        var data = new
        {
            // insert a smiley icon
            simple_prop = new RawXmlContent("<w:sym w:font=\"Wingdings\" w:char=\"F04A\"/>")
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var sym = Assert.Single(docXml!.Descendants(W + "sym"));
        Assert.Equal("Wingdings", sym.Attribute(W + "font")?.Value);
        Assert.Equal(W + "r", sym.Parent!.Name);
        Assert.Empty(docXml.Descendants(W + "t"));
    }

    [Fact]
    public async Task ReplacesAParagraphWithRawXmlContent()
    {
        var template = ReadFixture("simple.docx");
        var data = new
        {
            // insert a table
            simple_prop = new RawXmlContent(
                "<w:tbl><w:tr><w:tc><w:p><w:r><w:t>Hello</w:t></w:r></w:p></w:tc></w:tr></w:tbl>",
                replaceParagraph: true)
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var table = Assert.Single(docXml!.Descendants(W + "tbl"));
        Assert.Equal(W + "body", table.Parent!.Name);
        Assert.Equal("Hello", (await handler.GetTextAsync(doc)).Trim());
    }

    [Fact]
    public async Task SupportsListAsInput()
    {
        var template = ReadFixture("simple.docx");
        var data = new
        {
            simple_prop = new RawXmlContent(new[] { "<w:sym w:font=\"Wingdings\" w:char=\"F04A\"/>" })
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        Assert.Single(docXml!.Descendants(W + "sym"));
    }

    [Fact]
    public async Task SupportsDictionaryInput()
    {
        var template = ReadFixture("simple.docx");
        var data = new Dictionary<string, object?>
        {
            ["simple_prop"] = new Dictionary<string, object?>
            {
                ["_type"] = "rawXml",
                ["xml"] = new[] { "<w:p><w:r><w:t>Paragraph 1</w:t></w:r></w:p>", "<w:p><w:r><w:t>Paragraph 2</w:t></w:r></w:p>" },
                ["replaceParagraph"] = true
            }
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var paragraphs = docXml!.Descendants(W + "p").Select(p => p.Value).ToList();
        Assert.Equal(new[] { "Paragraph 1", "Paragraph 2" }, paragraphs);
    }

    [Fact]
    public async Task ReplacesMultipleParagraphs()
    {
        var template = ReadFixture("simple.docx");
        var data = new
        {
            simple_prop = new RawXmlContent(new[]
            {
                "<w:p><w:r><w:t>Paragraph 1</w:t></w:r></w:p>",
                "<w:p><w:r><w:t>Paragraph 2</w:t></w:r></w:p>"
            }, replaceParagraph: true)
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var paragraphs = docXml!.Descendants(W + "p").Select(p => p.Value).ToList();
        Assert.Equal(new[] { "Paragraph 1", "Paragraph 2" }, paragraphs);
    }

    [Fact]
    public async Task WorksInsideALoop()
    {
        var template = ReadFixture("loop - simple.docx");
        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop = new RawXmlContent(new[] { "<w:p><w:r><w:t>Repl 1</w:t></w:r></w:p>" }, replaceParagraph: true) },
                new { simple_prop = new RawXmlContent(new[] { "<w:p><w:r><w:t>Repl 2</w:t></w:r></w:p>" }, replaceParagraph: true) }
            }
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("Repl 1Repl 2", docText.Replace("!", "").Trim());
    }

    [Fact]
    public async Task ThrowsOnInvalidXml()
    {
        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = new RawXmlContent("<w:p><w:r>") };

        var handler = new TemplateHandler();
        await Assert.ThrowsAsync<Errors.TemplateDataException>(() => handler.ProcessAsync(template, data));
    }
}
