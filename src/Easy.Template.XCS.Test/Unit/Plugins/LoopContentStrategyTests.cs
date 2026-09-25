using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Plugins.Loop.Strategy;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Unit.Plugins;

public class LoopContentStrategyTests
{
    [Fact]
    public void SplitBefore_ClosingLoopTagHasExtraContentBeforeIt_TheExtraContentIsPreserved()
    {
        // bug #36 in easy-template-x
        var body = new Body(
            new Paragraph(
                new Run(new Text("{#loop}")),
                new Run(new Text("before"), new Text("{/loop}"), new Text("after"))));

        var openTag = new TextNodeTag
        {
            Name = "loop",
            Disposition = TagDisposition.Open,
            RawText = "{#loop}",
            XmlTextNode = TextAt(body, 0, 0, 0)
        };
        var closeTag = new TextNodeTag
        {
            Name = "loop",
            Disposition = TagDisposition.Close,
            RawText = "{/loop}",
            XmlTextNode = TextAt(body, 0, 1, 1)
        };
        Assert.Equal("{#loop}", openTag.XmlTextNode.Text);
        Assert.Equal("{/loop}", closeTag.XmlTextNode.Text);

        var strategy = new LoopContentStrategy();
        var result = strategy.SplitBefore(openTag, closeTag);

        var paragraph = Assert.Single(result.NodesToRepeat);
        var run = paragraph.ChildElements.FirstOrDefault();
        var textNode = Assert.IsType<Text>(run?.ChildElements.FirstOrDefault());
        Assert.Equal("before", textNode.Text);

        // the content after the closing tag stays in the last paragraph
        Assert.Equal("after", result.LastNode.InnerText);
        Assert.Equal("", result.FirstNode.InnerText);
    }

    [Fact]
    public void SplitBeforeAndMergeBack_MultipleParagraphs()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("Before1 {#loop} After1"))),
            new Paragraph(new Run(new Text("Middle"))),
            new Paragraph(new Run(new Text("Before3 {/loop} After3"))));

        // normalize the tags into their own text nodes
        var compiler = new TemplateCompiler(
            new Easy.Template.XCS.Compilation.Delimiters.DelimiterSearcher(new Delimiters(), 20),
            new TagParser(new Delimiters()),
            Easy.Template.XCS.Plugins.DefaultPlugins.Create(),
            new TemplateCompilerOptions { DefaultContentType = "text", ContainerContentType = "loop" });
        var tags = compiler.ParseTags(body);
        var openTag = (TextNodeTag)tags[0];
        var closeTag = (TextNodeTag)tags[1];

        var strategy = new LoopContentStrategy();
        var result = strategy.SplitBefore(openTag, closeTag);

        Assert.Equal(3, result.NodesToRepeat.Count);
        Assert.Equal(" After1", result.NodesToRepeat[0].InnerText);
        Assert.Equal("Middle", result.NodesToRepeat[1].InnerText);
        Assert.Equal("Before3 ", result.NodesToRepeat[2].InnerText);
        Assert.Equal("Before1 ", result.FirstNode.InnerText);
        Assert.Equal(" After3", result.LastNode.InnerText);
        Assert.Equal(2, body.ChildElements.Count);

        var groups = new List<List<OpenXmlElement>>
        {
            result.NodesToRepeat.Select(n => n.CloneNode(true)).ToList(),
            result.NodesToRepeat.Select(n => n.CloneNode(true)).ToList()
        };
        strategy.MergeBack(groups, result.FirstNode, result.LastNode);

        var texts = body.Elements<Paragraph>().Select(p => p.InnerText).ToList();
        Assert.Equal(new[] { "Before1  After1", "Middle", "Before3  After1", "Middle", "Before3  After3" }, texts);
    }
}
