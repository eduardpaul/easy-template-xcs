using Easy.Template.XCS.Plugins.Link;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class LinkFixtureTests
{
    [Fact]
    public async Task InsertsHyperLink()
    {
        var template = ReadFixture("link.docx");
        var data = new
        {
            link = new LinkContent
            {
                Text = "It's easy...",
                Target = "https://github.com/alonrbar/easy-template-x",
                Tooltip = "Click to open link"
            }
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        // the fixture contains the {link} tag four times, in various contexts
        var docXml = await handler.GetXmlAsync(doc);
        var hyperlinks = docXml!.Descendants(W + "hyperlink").ToList();
        Assert.Equal(4, hyperlinks.Count);

        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        foreach (var hyperlink in hyperlinks)
        {
            Assert.Equal("Click to open link", hyperlink.Attribute(W + "tooltip")?.Value);
            Assert.Contains(hyperlink.Attribute(W + "history")?.Value, new[] { "1", "true" });
            Assert.Equal("It's easy...", hyperlink.Value);
            Assert.Equal("Hyperlink", hyperlink.Descendants(W + "rStyle").Single().Attribute(W + "val")?.Value);

            // the relationship must exist and point to the target
            var relId = hyperlink.Attribute(R + "id")?.Value;
            var rel = docx.MainDocumentPart!.HyperlinkRelationships.Single(r => r.Id == relId);
            Assert.True(rel.IsExternal);
            Assert.Equal("https://github.com/alonrbar/easy-template-x", rel.Uri.ToString());
        }

        // the original run style is preserved (the third link is red in the template)
        Assert.Contains(hyperlinks, h => h.Descendants(W + "color").Any(c => c.Attribute(W + "val")?.Value == "FF0000"));

        // surrounding text is preserved
        var docText = await handler.GetTextAsync(doc);
        Assert.DoesNotContain("{link}", docText);
        Assert.Contains("Hi It's easy... hello", docText);
    }

    [Fact]
    public async Task InsertsHyperLink_FromDictionary()
    {
        var template = ReadFixture("link.docx");
        var data = new Dictionary<string, object?>
        {
            ["link"] = new Dictionary<string, object?>
            {
                ["_type"] = "link",
                ["target"] = "https://example.com"
            }
        };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var hyperlinks = docXml!.Descendants(W + "hyperlink").ToList();
        Assert.Equal(4, hyperlinks.Count);
        Assert.All(hyperlinks, h => Assert.Equal("https://example.com", h.Value));
    }

    [Fact]
    public async Task RemovesTagIfTargetIsMissing()
    {
        var template = ReadFixture("link.docx");
        var data = new { link = new LinkContent { Text = "no target" } };

        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        Assert.Empty(docXml!.Descendants(W + "hyperlink"));
        Assert.DoesNotContain("{link}", await handler.GetTextAsync(doc));
    }
}
