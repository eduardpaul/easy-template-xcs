using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Compilation.Delimiters;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Plugins;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Unit.Compilation;

public class TemplateCompilerTests
{
    private static TemplateCompiler CreateTemplateCompiler()
    {
        var delimiters = new Delimiters();
        return new TemplateCompiler(
            new DelimiterSearcher(delimiters, 20),
            new TagParser(delimiters),
            DefaultPlugins.Create(),
            new TemplateCompilerOptions
            {
                DefaultContentType = "text",
                ContainerContentType = "loop"
            });
    }

    [Fact]
    public void ParseTags_Simple()
    {
        var compiler = CreateTemplateCompiler();
        var paragraph = new Paragraph(new Run(new Text("{#loop}{/loop}")));

        var tags = compiler.ParseTags(paragraph);

        Assert.Equal(2, tags.Count);
        Assert.Equal("loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("loop", tags[1].Name);
        Assert.Equal(TagDisposition.Close, tags[1].Disposition);
    }

    [Fact]
    public void ParseTags_InlineDrawingInTheMiddleOfATextTagBreaksTheTag()
    {
        var compiler = CreateTemplateCompiler();
        var paragraph = ParseXml<Paragraph>("""
            <w:p>
                <w:r>
                    <w:t xml:space="preserve">{Text </w:t>
                </w:r>
                <w:r>
                    <w:drawing>
                        <wp:inline>
                            <wp:docPr id="1" name="Picture 1" descr="{Attribute Tag}"/>
                        </wp:inline>
                    </w:drawing>
                </w:r>
                <w:r>
                    <w:t>Tag}</w:t>
                </w:r>
            </w:p>
            """);

        Assert.Throws<MissingCloseDelimiterException>(() => compiler.ParseTags(paragraph));
    }

    [Fact]
    public void ParseTags_FloatingDrawingInTheMiddleOfATagIsIgnored()
    {
        var compiler = CreateTemplateCompiler();
        var paragraph = ParseXml<Paragraph>("""
            <w:p>
                <w:r>
                    <w:t>{Text </w:t>
                </w:r>
                <w:r>
                    <w:drawing>
                        <wp:anchor>
                            <wp:docPr id="1" name="Picture 1" descr="{Attribute Tag}"/>
                        </wp:anchor>
                    </w:drawing>
                </w:r>
                <w:r>
                    <w:t>Tag}</w:t>
                </w:r>
            </w:p>
            """);

        var tags = compiler.ParseTags(paragraph);

        Assert.Equal(2, tags.Count);
        Assert.Equal("Attribute Tag", tags[0].Name);
        Assert.IsType<AttributeTag>(tags[0]);
        Assert.Equal("Text Tag", tags[1].Name);
        Assert.IsType<TextNodeTag>(tags[1]);
    }

    [Fact]
    public async Task Compile_UnclosedLoopThrowsUnclosedTagException()
    {
        var compiler = CreateTemplateCompiler();
        var paragraph = new Paragraph(new Run(new Text("{#loop}{tag}")));
        var context = CreateContext();

        await Assert.ThrowsAsync<UnclosedTagException>(() => compiler.CompileAsync(paragraph, new ScopeData(new { }), context));
    }

    [Fact]
    public async Task Compile_UnknownContentTypeThrowsUnknownContentTypeException()
    {
        var compiler = CreateTemplateCompiler();
        var paragraph = new Paragraph(new Run(new Text("{tag}")));
        var context = CreateContext();
        var data = new Dictionary<string, object?>
        {
            ["tag"] = new Dictionary<string, object?> { ["_type"] = "no-such-plugin" }
        };

        var error = await Assert.ThrowsAsync<UnknownContentTypeException>(() => compiler.CompileAsync(paragraph, new ScopeData(data), context));
        Assert.Equal("no-such-plugin", error.ContentType);
        Assert.Equal("{tag}", error.TagRawText);
        Assert.Equal("tag", error.Path);
    }

    private static TemplateContext CreateContext()
    {
        var stream = new MemoryStream(ReadFixture("simple.docx"));
        var docx = WordprocessingDocument.Open(stream, false);
        return new TemplateContext
        {
            Docx = docx,
            CurrentPart = docx.MainDocumentPart!,
            Options = new TemplateOptions { MaxXmlDepth = 20 }
        };
    }
}
