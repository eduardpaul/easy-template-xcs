using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Plugins.RawXml;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class CustomDelimitersFixtureTests
{
    [Fact]
    public async Task ProcessCorrectlyATemplateWithCustomTagAndContainerDelimiters()
    {
        var handler = new TemplateHandler(new TemplateHandlerOptions
        {
            Delimiters = new Delimiters
            {
                TagStart = "{{",
                TagEnd = "}}",
                ContainerTagOpen = ">>",
                ContainerTagClose = "<<"
            }
        });
        var template = ReadFixture("custom delimiters.docx");
        var data = new Dictionary<string, object?>
        {
            ["@New Page"] = new RawXmlContent("<w:br w:type=\"page\"/>"),
            ["Students"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["Student Name"] = "Alon Bar",
                    ["Groups"] = new[]
                    {
                        new Dictionary<string, object?> { ["Group Name"] = "Math", ["Evaluation: Grade"] = 100, ["Evaluation: Teacher Comments"] = "Very good!" },
                        new Dictionary<string, object?> { ["Group Name"] = "English", ["Evaluation: Grade"] = 95, ["Evaluation: Teacher Comments"] = "Great!" }
                    }
                },
                new Dictionary<string, object?>
                {
                    ["Student Name"] = "David Blum",
                    ["Groups"] = new[]
                    {
                        new Dictionary<string, object?> { ["Group Name"] = "Math", ["Evaluation: Grade"] = 99, ["Evaluation: Teacher Comments"] = "Consequuntur magni sit officia." },
                        new Dictionary<string, object?> { ["Group Name"] = "English", ["Evaluation: Grade"] = 98, ["Evaluation: Teacher Comments"] = "Commodi alias in." }
                    }
                }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.DoesNotContain("{{", docText);
        Assert.Contains("Alon Bar", docText);
        Assert.Contains("David Blum", docText);
        Assert.Contains("Very good!", docText);
        Assert.Contains("Commodi alias in.", docText);

        var docXml = await handler.GetXmlAsync(doc);
        Assert.NotEmpty(docXml!.Descendants(W + "br").Where(br => br.Attribute(W + "type")?.Value == "page"));
    }
}

public class NoTagsFixtureTests
{
    [Fact]
    public async Task DoesNotThrowOnATemplateWithoutTags()
    {
        var template = ReadFixture("no tags.docx");
        var handler = new TemplateHandler();
        await handler.ProcessAsync(template, new { });
    }

    [Fact]
    public async Task DoesNotAlterADocumentWithoutTags()
    {
        var template = ReadFixture("no tags.docx");
        var handler = new TemplateHandler();

        var templateXml = await handler.GetXmlAsync(template);
        var doc = await handler.ProcessAsync(template, new { });
        var docXml = await handler.GetXmlAsync(doc);

        Assert.Equal(templateXml!.ToString(), docXml!.ToString());
    }
}

public class ContentControlsFixtureTests
{
    private static TemplateHandler CreateHandler() => new(new TemplateHandlerOptions
    {
        Delimiters = new Delimiters
        {
            TagStart = "{{",
            TagEnd = "}}",
            ContainerTagOpen = ">>",
            ContainerTagClose = "<<"
        }
    });

    // Possible support for content controls in the future, should probably be
    // behind a TemplateHandler option flag.

    [Fact]
    public async Task ParseTagsInContentControls()
    {
        var template = ReadFixture("content controls.docx");
        var handler = CreateHandler();

        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(CompareableText(@"
            Standard
            Plain text: Click or tap here to enter text.
            Rich text: Click or tap here to enter text.
            Picture:
            Checkbox: ☐
            Dropdown: Choose an item.
            Legacy
            Legacy text: FORMTEXT
            Legacy checkbox: FORMCHECKBOX
            Legacy dropdown: FORMDROPDOWN
            Standard - Text - Tags and Loop

            Hello {{ >> Loop1 }} Something1 {{ Tag1 }}{{ << }}
            Standard - Rich Text - Tags and Loop

            Hello {{ >> Loop2 }} Something2 {{ Tag2 }}{{ << }}
            Legacy - Text - Tags and Loop
            Hello FORMTEXT{{ >> Loop3 }} Something3 FORMTEXT{{ Tag3 }}{{ << }}
        "), CompareableText(templateText));

        var tags = await handler.ParseTagsAsync(template);
        Assert.Equal(9, tags.Count);
        Assert.Equal("{{ >> Loop1 }}", tags[0].RawText);
        Assert.Equal("{{ Tag1 }}", tags[1].RawText);
        Assert.Equal("{{ << }}", tags[2].RawText);
        Assert.Equal("{{ >> Loop2 }}", tags[3].RawText);
        Assert.Equal("{{ Tag2 }}", tags[4].RawText);
        Assert.Equal("{{ << }}", tags[5].RawText);
        Assert.Equal("{{ >> Loop3 }}", tags[6].RawText);
        Assert.Equal("{{ Tag3 }}", tags[7].RawText);
        Assert.Equal("{{ << }}", tags[8].RawText);
    }

    [Fact]
    public async Task ThrowsSyntaxErrorIfALoopTagIsFoundInsideAContentControl()
    {
        var template = ReadFixture("content controls.docx");
        var handler = CreateHandler();

        var error = await Assert.ThrowsAsync<TemplateSyntaxException>(() => handler.ProcessAsync(template, new { }));
        Assert.Contains("content control", error.Message);
    }
}

public class CommentsFixtureTests
{
    [Fact]
    public async Task ProcessCorrectlyATemplateWithComments()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("comments.docx");

        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{simple_prop}", templateText.Trim());

        var doc = await handler.ProcessAsync(template, new { simple_prop = "foo" });
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("foo", docText.Trim());

        // the tag was consolidated into a single run, the xml comment node did not break it
        var docXml = await handler.GetXmlAsync(doc);
        var texts = docXml!.Descendants(W + "t").Select(t => t.Value).ToList();
        Assert.Equal(new[] { "foo" }, texts);
    }
}

public class RelsFixtureTests
{
    [Fact]
    public async Task DoesNotThrowOnATemplateWithPrefixedRelsTargetPaths()
    {
        // Note: The test file for this test case is from Microsoft's online template library and originates from:
        // https://create.microsoft.com/en-us/template/invoice-(document)-f1603197-d3d8-44fc-95f7-0445aa29d9af
        var template = ReadFixture("rels variations.docx");
        var handler = new TemplateHandler();
        var doc = await handler.ProcessAsync(template, new { });

        // the output can be opened and still has its styles
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        Assert.NotNull(docx.MainDocumentPart!.StyleDefinitionsPart);
    }
}

public class RealLifeFixtureTests
{
    [Fact]
    public async Task HandlesARealLifeTemplateInHebrew()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("real life - he.docx");

        var students = new List<Dictionary<string, object?>>();
        const int studentsCount = 3;
        const int groupsCount = 3;
        for (var i = 0; i < studentsCount; i++)
        {
            var groups = new List<Dictionary<string, object?>>();
            for (var j = 0; j < groupsCount; j++)
            {
                groups.Add(new Dictionary<string, object?>
                {
                    ["שם הקבוצה"] = RandomWords(),
                    ["שם המורה"] = RandomWords(2),
                    ["הערכה מילולית"] = RandomParagraphs(2)
                });
            }
            students.Add(new Dictionary<string, object?>
            {
                ["שם התלמיד"] = RandomWords(),
                ["קבוצות"] = groups
            });
        }

        var data = new Dictionary<string, object?>
        {
            ["תלמידים"] = students,
            ["עמוד חדש"] = new RawXmlContent("<w:br w:type=\"page\"/>")
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.DoesNotContain("{", docText);
    }
}
