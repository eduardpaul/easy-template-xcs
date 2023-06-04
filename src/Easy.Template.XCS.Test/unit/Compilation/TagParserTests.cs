using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;

namespace test_easy_template_xcs;

public class TagParserTests
{
    [Fact]
    public void ParseSingleTag()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{tag}")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 4,
                XmlTextNode = textNode
            }};

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(1, tags.Length);

        // open tag
        Assert.Equal("tag", tags[0].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[0].Disposition);
        Assert.Equal("{tag}", tags[0].RawText);
    }

    [Fact]
    public void ParseSingleTag_BetweenText()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("start {tag} end")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 6,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 10,
                XmlTextNode = textNode
            }};

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(1, tags.Length);

        // open tag
        Assert.Equal("tag", tags[0].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[0].Disposition);
        Assert.Equal("{tag}", tags[0].RawText);
    }

    [Fact]
    public void ParseMultipleTags()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{tag}{tag2}")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 4,
                XmlTextNode = textNode
            },
             new DelimiterMark
            {
                IsOpen = true,
                Index = 5,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 10,
                XmlTextNode = textNode
            }};

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(2, tags.Length);

        // open tag
        Assert.Equal("tag", tags[0].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[0].Disposition);
        Assert.Equal("{tag}", tags[0].RawText);

        Assert.Equal("tag2", tags[1].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[1].Disposition);
        Assert.Equal("{tag2}", tags[1].RawText);
    }

    [Fact]
    public void ParseMultipleTags_BetweenText()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("start {tag} end {tag2} end2")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 6,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 10,
                XmlTextNode = textNode
            },
             new DelimiterMark
            {
                IsOpen = true,
                Index = 16,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 21,
                XmlTextNode = textNode
            }};

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(2, tags.Length);

        Assert.Equal("tag", tags[0].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[0].Disposition);
        Assert.Equal("{tag}", tags[0].RawText);

        Assert.Equal("tag2", tags[1].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[1].Disposition);
        Assert.Equal("{tag2}", tags[1].RawText);
    }

    [Fact]
    public void TrimsTagNames()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{# my loop  }{ my tag  }{/  my loop }")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 12,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index= 13,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen= false,
                Index = 23,
                XmlTextNode= textNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index = 24,
                XmlTextNode = textNode
                },
            new DelimiterMark{
                IsOpen= false,
                Index=36,
                XmlTextNode=textNode
            }
        };

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(3, tags.Length);

        // open tag
        Assert.Equal("my loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("{# my loop  }", tags[0].RawText);

        // middle tag
        Assert.Equal("my tag", tags[1].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[1].Disposition);
        Assert.Equal("{ my tag  }", tags[1].RawText);

        // close tag
        Assert.Equal("my loop", tags[2].Name);
        Assert.Equal(TagDisposition.Close, tags[2].Disposition);
        Assert.Equal("{/  my loop }", tags[2].RawText);
    }

    [Fact]
    public void ParseMultipleTags_BetweenText_LeadingText()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("text1{#loop}text2{/loop}text3")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 5,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 11,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index= 17,
                XmlTextNode = textNode
            },
            new DelimiterMark{
                IsOpen= false,
                Index = 23,
                XmlTextNode= textNode
            }
        };

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(2, tags.Length);

        // open tag
        Assert.Equal("loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("{#loop}", tags[0].RawText);

        // close tag
        Assert.Equal("loop", tags[1].Name);
        Assert.Equal(TagDisposition.Close, tags[1].Disposition);
        Assert.Equal("{/loop}", tags[1].RawText);
    }

    [Fact]
    public void ParseButterfly()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{#loop"),
                new Text("}{"),
                new Text("/loop}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[0].ChildElements[1];
        var thirdTextNode = (Text)paragraph.ChildElements[0].ChildElements[2];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 0,
                XmlTextNode = secondTextNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index= 1,
                XmlTextNode = secondTextNode
            },
            new DelimiterMark{
                IsOpen= false,
                Index = 5,
                XmlTextNode= thirdTextNode
            }
        };

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(2, tags.Length);

        // open tag
        Assert.Equal("loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("{#loop}", tags[0].RawText);

        // close tag
        Assert.Equal("loop", tags[1].Name);
        Assert.Equal(TagDisposition.Close, tags[1].Disposition);
        Assert.Equal("{/loop}", tags[1].RawText);
    }

    [Fact]
    public void ParseSingleTag_Splitted()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{#loo"),
                new Text("p}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[0].ChildElements[1];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 1,
                XmlTextNode = secondTextNode
            }
        };

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(1, tags.Length);

        // tag
        Assert.Equal("loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("{#loop}", tags[0].RawText);
    }

    [Fact]
    public void ParseSingleTag_SplittedClosing()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{#loop}{text}{/")
            ),
            new Run(
                new Text("loop")
            ),
            new Run(
                new Text("}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[1].ChildElements[0];
        var thirdTextNode = (Text)paragraph.ChildElements[2].ChildElements[0];

        var delimiters = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 6,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index= 7,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 12,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = true,
                Index= 13,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark{
                IsOpen = false,
                Index= 0,
                XmlTextNode = thirdTextNode
            }
        };

        var options = new TemplateHandlerOptions();
        var tagParser = new TagParser(options.Delimiters);
        var tags = tagParser.Parse(delimiters);

        Assert.Equal(3, tags.Length);

        // tag
        Assert.Equal("loop", tags[0].Name);
        Assert.Equal(TagDisposition.Open, tags[0].Disposition);
        Assert.Equal("{#loop}", tags[0].RawText);

        Assert.Equal("text", tags[1].Name);
        Assert.Equal(TagDisposition.SelfClosed, tags[1].Disposition);
        Assert.Equal("{text}", tags[1].RawText);

        Assert.Equal("loop", tags[2].Name);
        Assert.Equal(TagDisposition.Close, tags[2].Disposition);
        Assert.Equal("{/loop}", tags[2].RawText);
    }
}