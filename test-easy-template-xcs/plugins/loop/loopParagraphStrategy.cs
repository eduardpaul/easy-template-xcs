using DocumentFormat.OpenXml;
using EasyTemplateXCS.Compilation;
using EasyTemplateXCS.Plugins;
using EasyTemplateXCS.Plugins.LoopPlugin;
using DocumentFormat.OpenXml.Wordprocessing;
using Tag = EasyTemplateXCS.Compilation.Tag;

namespace test_easy_template_xcs;

public class LoopParagraphStrategyTests
{
    [Fact]
    public void SplitBefore_ClosingLoopTagHasExtraContent_ContentIsPreserved()
    {
        // prepare
        var body = new Body(
            new Paragraph(
                new Run(
                    new Text("{#loop}")
                ),
                new Run(
                    new Text("before"),
                    new Text("{/loop}"),
                    new Text("after")
                )));

        var delimiterSearcher = new DelimiterSearcher();
        var templateHandler = new TemplateHandler();
        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var compiler = new TemplateCompiler(
            delimiterSearcher,
            tagParser,
            options.Plugins,
            new TemplateCompilerOptions
            {
                SkipEmptyTags = options.SkipEmptyTags,
                DefaultContentType = options.DefaultContentType,
                ContainerContentType = options.ContainerContentType
            }
        );

        var openTag = new Tag
        {
            Name = "loop",
            Disposition = TagDisposition.Open,
            RawText = "{#loop}",
            XmlTextNode = body.ChildElements[0].ChildElements[0].ChildElements[0] as Text
        };
        var closeTag = new Tag
        {
            Name = "loop",
            Disposition = TagDisposition.Open,
            RawText = "{/loop}",
            XmlTextNode = body.ChildElements[0].ChildElements[1].ChildElements[1] as Text
        };
        Assert.Equal("{#loop}", openTag.XmlTextNode.Text);
        Assert.Equal("{/loop}", closeTag.XmlTextNode.Text);

        var strategy = new LoopParagraphStrategy();
        var pluginUtilities = new PluginUtilities
        {
            Compiler = compiler
        };
        strategy.SetUtilities(pluginUtilities);

        // test
        var result = strategy.SplitBefore(openTag, closeTag);

        var nodesToRepeat = result.NodesToRepeat.ToList();
        Assert.Single(nodesToRepeat);

        var paragraph = nodesToRepeat[0];
        var run = paragraph.ChildElements?.FirstOrDefault();
        var wordTextNode = run?.ChildElements?.FirstOrDefault() as Text;
        Assert.NotNull(wordTextNode);
        Assert.Equal("before", wordTextNode.Text);
    }
}
