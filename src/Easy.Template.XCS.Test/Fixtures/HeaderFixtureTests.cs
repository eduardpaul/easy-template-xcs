using Easy.Template.XCS.Office;
using Easy.Template.XCS.Plugins.Image;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class HeaderFixtureTests
{
    [Fact]
    public async Task ReplacesSimpleTags()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("header and footer.docx");
        var data = new Dictionary<string, object?>
        {
            ["my header"] = "I'm in the header!",
            ["my body"] = "hello world",
            ["my footer"] = "Hello from down below"
        };

        var doc = await handler.ProcessAsync(template, data);

        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello world", docText.Trim());

        var headerText = await handler.GetTextAsync(doc, RelType.Header);
        Assert.Equal("I'm in the header!", headerText.Trim());

        var footerText = await handler.GetTextAsync(doc, RelType.Footer);
        Assert.Equal("Hello from down below", footerText.Trim());
    }

    [Fact]
    public async Task ReplacesLoopTags()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("header and footer - loop.docx");
        var data = new Dictionary<string, object?>
        {
            ["header loop"] = new[]
            {
                new Dictionary<string, object?> { ["my header"] = "I'm in the header!" },
                new Dictionary<string, object?> { ["my header"] = "Me too!" }
            },
            ["body loop"] = new[]
            {
                new Dictionary<string, object?> { ["my body"] = "hello world1" },
                new Dictionary<string, object?> { ["my body"] = "hello world2" }
            },
            ["footer loop"] = new[]
            {
                new Dictionary<string, object?> { ["my footer"] = "Hello from down below." },
                new Dictionary<string, object?> { ["my footer"] = "How do you do?" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);

        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello world1hello world2", docText.Trim());

        var headerText = await handler.GetTextAsync(doc, RelType.Header);
        Assert.Equal("I'm in the header!Me too!", headerText.Trim());

        var footerText = await handler.GetTextAsync(doc, RelType.Footer);
        Assert.Equal("Hello from down below.How do you do?", footerText.Trim());
    }

    [Fact]
    public async Task ReplacesImageTags()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("header and footer.docx");
        var data = new Dictionary<string, object?>
        {
            ["my header"] = new ImageContent { Format = MimeType.Jpeg, Source = ReadResource("panda1.jpg"), Height = 100, Width = 200 },
            ["my body"] = "hello world",
            ["my footer"] = new ImageContent { Format = MimeType.Png, Source = ReadResource("panda2.png"), Height = 100, Width = 100 }
        };

        var doc = await handler.ProcessAsync(template, data);

        // assert content changed
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hello world", docText.Trim());

        // (the fixture has several headers and footers, only one of each contains a tag)
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        var headerPart = Assert.Single(docx.MainDocumentPart!.HeaderParts, p => p.RootElement!.Descendants<Drawing>().Any());
        var footerPart = Assert.Single(docx.MainDocumentPart!.FooterParts, p => p.RootElement!.Descendants<Drawing>().Any());
        Assert.Single(headerPart.RootElement!.Descendants<Drawing>());
        Assert.Single(footerPart.RootElement!.Descendants<Drawing>());

        // assert image binary added
        var imageParts = docx.GetAllParts().OfType<ImagePart>().ToList();
        Assert.Single(imageParts, p => p.ContentType == MimeType.Jpeg);
        Assert.Single(imageParts, p => p.ContentType == MimeType.Png);

        // the header and footer reference their images
        var headerEmbed = headerPart.RootElement.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Single().Embed!.Value!;
        Assert.Equal(MimeType.Jpeg, ((ImagePart)headerPart.GetPartById(headerEmbed)).ContentType);
        var footerEmbed = footerPart.RootElement.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Single().Embed!.Value!;
        Assert.Equal(MimeType.Png, ((ImagePart)footerPart.GetPartById(footerEmbed)).ContentType);
    }

    [Fact]
    public async Task ProcessesHeaderAndFooterReferencesInTheMiddleOfTheDocument()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("header and footer - middle reference.docx");

        var headerTextBefore = await handler.GetTextAsync(template, RelType.Header);
        Assert.Contains("client.court.courtFile", headerTextBefore);
        Assert.DoesNotContain("TEST", headerTextBefore);

        var data = new Dictionary<string, object?> { ["client.court.courtFile"] = "TEST" };
        var doc = await handler.ProcessAsync(template, data);

        var headerTextAfter = await handler.GetTextAsync(doc, RelType.Header);
        Assert.Contains("TEST", headerTextAfter);
        Assert.DoesNotContain("client.court.courtFile", headerTextAfter);
    }
}
