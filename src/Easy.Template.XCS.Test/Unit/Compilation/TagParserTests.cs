using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Compilation.Delimiters;
using Easy.Template.XCS.Errors;
using static Easy.Template.XCS.Test.TestUtils;
using Tag = Easy.Template.XCS.Compilation.Tag;

namespace Easy.Template.XCS.Test.Unit.Compilation;

public class TagParserTests
{
    private static TagParser CreateTagParser(Delimiters? delimiters = null)
    {
        return new TagParser(new Delimiters(delimiters));
    }

    private static TextNodeDelimiterMark Mark(bool isOpen, int index, Text textNode)
    {
        return new TextNodeDelimiterMark { IsOpen = isOpen, Index = index, XmlTextNode = textNode };
    }

    private static AttributeDelimiterMark AttrMark(bool isOpen, int index, OpenXmlElement node, string attributeName = "descr")
    {
        return new AttributeDelimiterMark { IsOpen = isOpen, Index = index, XmlNode = node, AttributeName = attributeName };
    }

    private static void AssertTag(Tag tag, string name, TagDisposition disposition, string rawText)
    {
        Assert.Equal(name, tag.Name);
        Assert.Equal(disposition, tag.Disposition);
        Assert.Equal(rawText, tag.RawText);
    }

    //
    // text node tags
    //

    [Fact]
    public void ParseSingleTag()
    {
        var paragraph = new Paragraph(new Run(new Text("{tag}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, textNode), Mark(false, 4, textNode) };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Single(tags);
        AssertTag(tags[0], "tag", TagDisposition.SelfClosed, "{tag}");
        Assert.Same(textNode, Assert.IsType<TextNodeTag>(tags[0]).XmlTextNode);
    }

    [Fact]
    public void ParseSingleTag_BetweenText()
    {
        var paragraph = new Paragraph(new Run(new Text("start {tag} end")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 6, textNode), Mark(false, 10, textNode) };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Single(tags);
        AssertTag(tags[0], "tag", TagDisposition.SelfClosed, "{tag}");

        // the text was split into three text nodes
        var texts = paragraph.Descendants<Text>().Select(t => t.Text).ToList();
        Assert.Equal(new[] { "start ", "{tag}", " end" }, texts);
        Assert.All(paragraph.Descendants<Text>(), t => Assert.Equal(SpaceProcessingModeValues.Preserve, t.Space?.Value));
    }

    [Fact]
    public void TrimsTagNames()
    {
        var paragraph = new Paragraph(new Run(new Text("{# my loop  }{ my tag  }{/  my loop }")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, textNode), Mark(false, 12, textNode),
            Mark(true, 13, textNode), Mark(false, 23, textNode),
            Mark(true, 24, textNode), Mark(false, 36, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(3, tags.Count);
        AssertTag(tags[0], "my loop", TagDisposition.Open, "{# my loop  }");
        AssertTag(tags[1], "my tag", TagDisposition.SelfClosed, "{ my tag  }");
        AssertTag(tags[2], "my loop", TagDisposition.Close, "{/  my loop }");
    }

    [Fact]
    public void MultipleTagsOnTheSameTextNode()
    {
        var paragraph = new Paragraph(new Run(new Text("{tag}{tag2}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, textNode), Mark(false, 4, textNode),
            Mark(true, 5, textNode), Mark(false, 10, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "tag", TagDisposition.SelfClosed, "{tag}");
        AssertTag(tags[1], "tag2", TagDisposition.SelfClosed, "{tag2}");
    }

    [Fact]
    public void MultipleTagsOnTheSameTextNode_WithLeadingText()
    {
        var paragraph = new Paragraph(new Run(new Text("text1{#loop}text2{/loop}text3")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 5, textNode), Mark(false, 11, textNode),
            Mark(true, 17, textNode), Mark(false, 23, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{#loop}");
        AssertTag(tags[1], "loop", TagDisposition.Close, "{/loop}");

        var texts = paragraph.Descendants<Text>().Select(t => t.Text).ToList();
        Assert.Equal(new[] { "text1", "{#loop}", "text2", "{/loop}", "text3" }, texts);
    }

    [Fact]
    public void ParseAButterfly()
    {
        var paragraph = new Paragraph(new Run(new Text("{#loop"), new Text("}{"), new Text("/loop}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var secondTextNode = TextAt(paragraph, 0, 1);
        var thirdTextNode = TextAt(paragraph, 0, 2);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, firstTextNode), Mark(false, 0, secondTextNode),
            Mark(true, 1, secondTextNode), Mark(false, 5, thirdTextNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{#loop}");
        AssertTag(tags[1], "loop", TagDisposition.Close, "{/loop}");
    }

    [Fact]
    public void SplittedSimpleTag()
    {
        var paragraph = new Paragraph(new Run(new Text("{#loo"), new Text("p}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var secondTextNode = TextAt(paragraph, 0, 1);
        var delimiters = new DelimiterMark[] { Mark(true, 0, firstTextNode), Mark(false, 1, secondTextNode) };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Single(tags);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{#loop}");
    }

    [Fact]
    public void SplittedClosingTag()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{#loop}{text}{/")),
            new Run(new Text("loop")),
            new Run(new Text("}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var thirdTextNode = TextAt(paragraph, 2, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, firstTextNode), Mark(false, 6, firstTextNode),
            Mark(true, 7, firstTextNode), Mark(false, 12, firstTextNode),
            Mark(true, 13, firstTextNode), Mark(false, 0, thirdTextNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(3, tags.Count);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{#loop}");
        AssertTag(tags[1], "text", TagDisposition.SelfClosed, "{text}");
        AssertTag(tags[2], "loop", TagDisposition.Close, "{/loop}");
    }

    [Fact]
    public void CloseDelimiterInDifferentParagraphThrowsMissingCloseDelimiterException()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("{tag"))),
            new Paragraph(new Run(new Text("}"))));
        var firstTextNode = TextAt(body, 0, 0, 0);
        var secondTextNode = TextAt(body, 1, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, firstTextNode), Mark(false, 0, secondTextNode) };

        Assert.Throws<MissingCloseDelimiterException>(() => CreateTagParser().Parse(delimiters));
    }

    [Fact]
    public void CloseBeforeOpenThrowsMissingStartDelimiterException()
    {
        var paragraph = new Paragraph(new Run(new Text("}tag{")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(false, 0, textNode), Mark(true, 4, textNode) };

        var error = Assert.Throws<MissingStartDelimiterException>(() => CreateTagParser().Parse(delimiters));
        Assert.Equal("}tag{", error.CloseDelimiterText);
    }

    [Fact]
    public void OpenBeforeCloseThrowsMissingCloseDelimiterException()
    {
        var paragraph = new Paragraph(new Run(new Text("{tag{")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, textNode), Mark(true, 4, textNode) };

        var error = Assert.Throws<MissingCloseDelimiterException>(() => CreateTagParser().Parse(delimiters));
        Assert.Equal("{tag{", error.OpenDelimiterText);
    }

    //
    // attribute tags
    //

    [Fact]
    public void SimpleAttributeTag()
    {
        var docPr = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "{tag}" };
        var delimiters = new DelimiterMark[] { AttrMark(true, 0, docPr), AttrMark(false, 4, docPr) };

        var tags = CreateTagParser().Parse(delimiters);

        var tag = Assert.IsType<AttributeTag>(Assert.Single(tags));
        AssertTag(tag, "tag", TagDisposition.SelfClosed, "{tag}");
        Assert.Same(docPr, tag.XmlNode);
        Assert.Equal("descr", tag.AttributeName);
    }

    [Fact]
    public void MultipleSelfClosedTagsInAttribute()
    {
        var docPr = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "Hello {tag1} {tag2}" };
        var delimiters = new DelimiterMark[]
        {
            AttrMark(true, 6, docPr), AttrMark(false, 11, docPr),
            AttrMark(true, 13, docPr), AttrMark(false, 18, docPr)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "tag1", TagDisposition.SelfClosed, "{tag1}");
        AssertTag(tags[1], "tag2", TagDisposition.SelfClosed, "{tag2}");
    }

    [Fact]
    public void TextAndAttributeDelimitersAreParsedIndependently()
    {
        var paragraph = new Paragraph(new Run(new Text("{text tag}")));
        var textNode = TextAt(paragraph, 0, 0);
        var docPr = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "{attr tag}" };
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, textNode),
            AttrMark(true, 0, docPr), AttrMark(false, 9, docPr),
            Mark(false, 9, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "attr tag", TagDisposition.SelfClosed, "{attr tag}");
        AssertTag(tags[1], "text tag", TagDisposition.SelfClosed, "{text tag}");
    }

    [Fact]
    public void MissingOpenDelimiterInAttributeThrowsMissingStartDelimiterException()
    {
        var docPr = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "tag}" };
        var delimiters = new DelimiterMark[] { AttrMark(false, 3, docPr) };

        Assert.Throws<MissingStartDelimiterException>(() => CreateTagParser().Parse(delimiters));
    }

    [Fact]
    public void OpenAndCloseTagsInDifferentAttributesThrowsMissingCloseDelimiterException()
    {
        var docPr1 = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "{tag" };
        var docPr2 = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 2U, Name = "Picture 2", Description = "}" };
        var delimiters = new DelimiterMark[] { AttrMark(true, 0, docPr1), AttrMark(false, 0, docPr2) };

        Assert.Throws<MissingCloseDelimiterException>(() => CreateTagParser().Parse(delimiters));
    }

    //
    // tag options
    //

    [Fact]
    public void TagOptions_Simple()
    {
        var paragraph = new Paragraph(new Run(new Text("{#loop [opt: \"yes\"]}{/loop}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, textNode), Mark(false, 19, textNode),
            Mark(true, 20, textNode), Mark(false, 26, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{#loop [opt: \"yes\"]}");
        Assert.Equal(new Dictionary<string, object?> { ["opt"] = "yes" }, tags[0].Options);
        AssertTag(tags[1], "loop", TagDisposition.Close, "{/loop}");
        Assert.Null(tags[1].Options);
    }

    [Fact]
    public void TagOptions_WithWhitespace()
    {
        var paragraph = new Paragraph(new Run(new Text("{ # loop [opt: \"yes\"] }{ / loop }")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[]
        {
            Mark(true, 0, textNode), Mark(false, 22, textNode),
            Mark(true, 23, textNode), Mark(false, 32, textNode)
        };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal(2, tags.Count);
        AssertTag(tags[0], "loop", TagDisposition.Open, "{ # loop [opt: \"yes\"] }");
        Assert.Equal(new Dictionary<string, object?> { ["opt"] = "yes" }, tags[0].Options);
        AssertTag(tags[1], "loop", TagDisposition.Close, "{ / loop }");
    }

    [Fact]
    public void TagOptions_CurlyQuotesAreNormalized()
    {
        var paragraph = new Paragraph(new Run(new Text("{#loop [loopOver: “row”]}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, textNode), Mark(false, 24, textNode) };

        var tags = CreateTagParser().Parse(delimiters);

        Assert.Equal("row", Assert.Single(tags).Options?["loopOver"]);
    }

    [Fact]
    public void TagOptions_AngularParserStyleWithBrackets()
    {
        var paragraph = new Paragraph(new Run(new Text("{something[0] [[myOpt: 5]]}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, textNode), Mark(false, 26, textNode) };

        var tags = CreateTagParser(new Delimiters { TagOptionsStart = "[[", TagOptionsEnd = "]]" }).Parse(delimiters);

        var tag = Assert.Single(tags);
        AssertTag(tag, "something[0]", TagDisposition.SelfClosed, "{something[0] [[myOpt: 5]]}");
        Assert.Equal(5L, tag.Options?["myOpt"]);
    }

    [Fact]
    public void TagOptions_InvalidOptions()
    {
        var paragraph = new Paragraph(new Run(new Text("{something [myOpt 5]}")));
        var textNode = TextAt(paragraph, 0, 0);
        var delimiters = new DelimiterMark[] { Mark(true, 0, textNode), Mark(false, 20, textNode) };

        var error = Assert.Throws<TagOptionsParseException>(() => CreateTagParser().Parse(delimiters));
        Assert.Equal("{something [myOpt 5]}", error.TagRawText);
        Assert.NotNull(error.InnerException);
    }
}
