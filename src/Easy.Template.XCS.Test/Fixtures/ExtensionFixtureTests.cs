using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Extensions;
using Easy.Template.XCS.Plugins;
using static Easy.Template.XCS.Test.TestUtils;
using Tag = Easy.Template.XCS.Compilation.Tag;

namespace Easy.Template.XCS.Test.Fixtures;

public class ExtensionFixtureTests
{
    private sealed class RecordingExtension : TemplateExtension
    {
        public List<string> Calls { get; } = new();

        public override Task ExecuteAsync(ScopeData data, TemplateContext context)
        {
            Assert.NotNull(Utilities.Compiler);
            Assert.NotNull(Utilities.TagParser);
            var text = context.CurrentPart.RootElement?.InnerText ?? "";
            Calls.Add($"{context.CurrentPart.Uri.OriginalString}: {text.Trim()}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExtensionsAreCalledBeforeAndAfterCompilationOfEachPart()
    {
        var before = new RecordingExtension();
        var after = new RecordingExtension();
        var handler = new TemplateHandler(new TemplateHandlerOptions
        {
            Extensions = new ExtensionOptions
            {
                BeforeCompilation = { before },
                AfterCompilation = { after }
            }
        });

        var template = ReadFixture("simple.docx");
        await handler.ProcessAsync(template, new { simple_prop = "hello" });

        Assert.Equal(new[] { "/word/document.xml: {simple_prop}" }, before.Calls);
        Assert.Equal(new[] { "/word/document.xml: hello" }, after.Calls);
    }

    private sealed class UpperCasePlugin : TemplatePlugin
    {
        public override string ContentType => "upper";

        public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
        {
            var textTag = Assert.IsType<TextNodeTag>(tag);
            var value = data.GetScopeData<UpperContent>();
            textTag.XmlTextNode.Text = (value?.Value ?? "").ToUpperInvariant();
            return Task.CompletedTask;
        }
    }

    private sealed class UpperContent : PluginContent
    {
        public override string ContentType => "upper";
        public string? Value { get; set; }
    }

    [Fact]
    public async Task CustomPluginsCanBeRegistered()
    {
        var options = new TemplateHandlerOptions();
        options.Plugins.Add(new UpperCasePlugin());
        var handler = new TemplateHandler(options);

        var template = ReadFixture("simple.docx");
        var doc = await handler.ProcessAsync(template, new { simple_prop = new UpperContent { Value = "shout" } });
        Assert.Equal("SHOUT", (await handler.GetTextAsync(doc)).Trim());

        // dictionaries with a "_type" work too
        doc = await handler.ProcessAsync(template, new Dictionary<string, object?>
        {
            ["simple_prop"] = new Dictionary<string, object?> { ["_type"] = "upper", ["value"] = "louder" }
        });
        Assert.Equal("LOUDER", (await handler.GetTextAsync(doc)).Trim());
    }

    [Fact]
    public async Task CustomScopeDataResolver()
    {
        var handler = new TemplateHandler(new TemplateHandlerOptions
        {
            ScopeDataResolver = args => $"resolved:{string.Join(".", args.StrPath)}"
        });

        var template = ReadFixture("simple.docx");
        var doc = await handler.ProcessAsync(template, new { });
        Assert.Equal("resolved:simple_prop", (await handler.GetTextAsync(doc)).Trim());
    }

    [Fact]
    public void OptionsAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new TemplateHandler(new TemplateHandlerOptions { Plugins = new List<TemplatePlugin>() }));
        Assert.Throws<ArgumentException>(() => new TemplateHandler(new TemplateHandlerOptions { Delimiters = new Delimiters { TagStart = " {" } }));
        Assert.Throws<ArgumentException>(() => new TemplateHandler(new TemplateHandlerOptions { Delimiters = new Delimiters { ContainerTagOpen = "#", ContainerTagClose = "#" } }));
        Assert.Throws<ArgumentException>(() => new TemplateHandler(new TemplateHandlerOptions { MaxXmlDepth = 0 }));
    }

    [Fact]
    public async Task MalformedFileThrowsMalformedFileException()
    {
        var handler = new TemplateHandler();
        await Assert.ThrowsAsync<Errors.MalformedFileException>(() => handler.ProcessAsync(new byte[] { 1, 2, 3 }, new { }));
    }

    [Fact]
    public void VersionIsAvailable()
    {
        Assert.False(string.IsNullOrEmpty(TemplateHandler.Version));
    }
}
