using DocumentFormat.OpenXml.Validation;
using Easy.Template.XCS.Plugins.Image;
using Easy.Template.XCS.Plugins.Link;
using Easy.Template.XCS.Plugins.RawXml;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

/// <summary>
/// Validates the generated documents against the OpenXml schema, to make
/// sure the generated markup is accepted by Word.
/// </summary>
public class ValidationFixtureTests
{
    private static List<string> Validate(byte[] doc)
    {
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        return validator.Validate(docx)
            .Select(e => $"{e.Part?.Uri}: {e.Description} [{e.Path?.XPath}]")
            .ToList();
    }

    private static async Task AssertNoNewValidationErrors(string fixture, object data, TemplateHandlerOptions? options = null)
    {
        var handler = new TemplateHandler(options);
        var template = ReadFixture(fixture);
        var templateErrors = Validate(template);

        var doc = await handler.ProcessAsync(template, data);
        var docErrors = Validate(doc);

        var newErrors = docErrors.Except(templateErrors).ToList();
        Assert.True(newErrors.Count == 0, "New validation errors:\n" + string.Join("\n", newErrors));
    }

    [Fact]
    public Task Text_MultiLine() => AssertNoNewValidationErrors("simple.docx", new { simple_prop = "line 1\nline 2\n\nline 4" });

    [Fact]
    public Task Loop_Paragraphs() => AssertNoNewValidationErrors("loop - paragraph - multi line - without loopOver.docx", new
    {
        loop_prop = new[] { new { simple_prop = "FIRST" }, new { simple_prop = "SECOND" } }
    });

    [Fact]
    public Task Loop_TableRows() => AssertNoNewValidationErrors("loop - table.docx", new
    {
        loop = new[] { new { prop = "first" }, new { prop = "second" } }
    });

    [Fact]
    public Task Loop_TableColumns() => AssertNoNewValidationErrors("loop - table - columns.docx", new
    {
        loop = new[] { new { prop1 = "a", prop2 = "b" }, new { prop1 = "c", prop2 = "d" } }
    });

    [Fact]
    public Task Loop_EmptyTableCells() => AssertNoNewValidationErrors("loop - table - loopOver paragraph.docx", new
    {
        loop1 = Array.Empty<object>()
    });

    [Fact]
    public Task Loop_List() => AssertNoNewValidationErrors("loop - list.docx", new
    {
        loop1 = new[]
        {
            new { loop2 = new[] { new { prop = "first" }, new { prop = "second" } } },
            new { loop2 = new[] { new { prop = "third" }, new { prop = "forth" } } }
        }
    });

    [Fact]
    public Task Image_New() => AssertNoNewValidationErrors("simple.docx", new
    {
        simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = ReadResource("panda1.jpg"), Width = 100, Height = 50, TransparencyPercent = 20 }
    });

    [Fact]
    public Task Image_NewWithAltText() => AssertNoNewValidationErrors("simple.docx", new
    {
        simple_prop = new ImageContent { Format = MimeType.Png, Source = ReadResource("panda2.png"), Width = 100, Height = 50, AltText = "A panda" }
    });

    [Fact]
    public Task Image_Placeholder() => AssertNoNewValidationErrors("image - placeholder.docx", new Dictionary<string, object?>
    {
        ["My Tag 2"] = new ImageContent { Format = MimeType.Jpeg, Source = ReadResource("panda1.jpg"), Width = 100, Height = 50, TransparencyPercent = 20 }
    });

    [Fact]
    public Task Image_InHeaderAndFooter() => AssertNoNewValidationErrors("header and footer.docx", new Dictionary<string, object?>
    {
        ["my header"] = new ImageContent { Format = MimeType.Jpeg, Source = ReadResource("panda1.jpg"), Height = 100, Width = 200 },
        ["my body"] = "hello world",
        ["my footer"] = new ImageContent { Format = MimeType.Png, Source = ReadResource("panda2.png"), Height = 100, Width = 100 }
    });

    [Fact]
    public Task Link() => AssertNoNewValidationErrors("link.docx", new
    {
        link = new LinkContent("https://example.com", "Example", "tooltip")
    });

    [Fact]
    public Task RawXml() => AssertNoNewValidationErrors("simple.docx", new
    {
        simple_prop = new RawXmlContent("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t>Hello</w:t></w:r></w:p></w:tc></w:tr></w:tbl>", replaceParagraph: true)
    });
}
