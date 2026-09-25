using Easy.Template.XCS.Compilation.Delimiters;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Unit.Compilation;

public class DelimiterSearcherTests
{
    private static DelimiterSearcher CreateSearcher(string tagStart = "{", string tagEnd = "}")
    {
        return new DelimiterSearcher(new Delimiters { TagStart = tagStart, TagEnd = tagEnd }, 20);
    }

    private static void AssertTextDelimiter(DelimiterMark actual, bool isOpen, int index, Text textNode)
    {
        var textDelimiter = Assert.IsType<TextNodeDelimiterMark>(actual);
        Assert.Equal(isOpen, textDelimiter.IsOpen);
        Assert.Equal(index, textDelimiter.Index);
        Assert.Same(textNode, textDelimiter.XmlTextNode);
    }

    //
    // single character delimiters
    //

    [Fact]
    public void SingleCharacterDelimiters_SimpleParagraph()
    {
        var paragraph = new Paragraph(new Run(new Text("{#loop}{/loop}")));
        var textNode = TextAt(paragraph, 0, 0);

        var delimiters = CreateSearcher().FindDelimiters(paragraph);

        Assert.Equal(4, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, textNode);
        AssertTextDelimiter(delimiters[1], false, 6, textNode);
        AssertTextDelimiter(delimiters[2], true, 7, textNode);
        AssertTextDelimiter(delimiters[3], false, 13, textNode);
    }

    [Fact]
    public void SingleCharacterDelimiters_TwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(new Run(new Text("{#lo"), new Text("op}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var secondTextNode = TextAt(paragraph, 0, 1);

        var delimiters = CreateSearcher().FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 2, secondTextNode);
    }

    [Fact]
    public void SingleCharacterDelimiters_TwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{")),
            new Run(new Text("tag")),
            new Run(new Text("}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var thirdTextNode = TextAt(paragraph, 2, 0);

        var delimiters = CreateSearcher().FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 0, thirdTextNode);
    }

    [Fact]
    public void SingleCharacterDelimiters_InlineDrawingInTheMiddleOfATag()
    {
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
        var firstTextNode = TextAt(paragraph, 0, 0);
        var thirdTextNode = TextAt(paragraph, 2, 0);
        var docPr = paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().Single();

        var delimiters = CreateSearcher().FindDelimiters(paragraph);

        Assert.Equal(4, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);

        var attrOpen = Assert.IsType<AttributeDelimiterMark>(delimiters[1]);
        Assert.True(attrOpen.IsOpen);
        Assert.Equal(0, attrOpen.Index);
        Assert.Same(docPr, attrOpen.XmlNode);
        Assert.Equal("descr", attrOpen.AttributeName);

        var attrClose = Assert.IsType<AttributeDelimiterMark>(delimiters[2]);
        Assert.False(attrClose.IsOpen);
        Assert.Equal(14, attrClose.Index);

        // the inline drawing resets the match state - the closing delimiter is
        // found in the third run and not joined with the first one
        AssertTextDelimiter(delimiters[3], false, 3, thirdTextNode);
    }

    //
    // multi character delimiters
    //

    [Fact]
    public void MultiCharacterDelimiters_SimpleParagraph()
    {
        var paragraph = new Paragraph(new Run(new Text("{{#loop}}{{/loop}}")));
        var textNode = TextAt(paragraph, 0, 0);

        var delimiters = CreateSearcher("{{", "}}").FindDelimiters(paragraph);

        Assert.Equal(4, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, textNode);
        AssertTextDelimiter(delimiters[1], false, 7, textNode);
        AssertTextDelimiter(delimiters[2], true, 9, textNode);
        AssertTextDelimiter(delimiters[3], false, 16, textNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_TwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(new Run(new Text("{{#lo"), new Text("op}}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var secondTextNode = TextAt(paragraph, 0, 1);

        var delimiters = CreateSearcher("{{", "}}").FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 2, secondTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_TwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{{")),
            new Run(new Text("tag")),
            new Run(new Text("}}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var thirdTextNode = TextAt(paragraph, 2, 0);

        var delimiters = CreateSearcher("{{", "}}").FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 0, thirdTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_DelimitersSplittedAcrossSeveralDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{")),
            new Run(new Text("{{tag}")),
            new Run(new Text("}")),
            new Run(new Text("}")));
        var firstTextNode = TextAt(paragraph, 0, 0);

        var delimiters = CreateSearcher("{{{", "}}}").FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 6, firstTextNode);

        // the delimiter characters were joined into the first text node
        Assert.Equal("{{{tag}}}", firstTextNode.Text);
    }

    [Fact]
    public void MultiCharacterDelimiters_SplittedAcrossRuns_WithAttributeTagsOfAFloatingDrawingInTheMiddle()
    {
        var paragraph = ParseXml<Paragraph>("""
            <w:p>
                <w:r>
                    <w:t>{</w:t>
                </w:r>
                <w:r>
                    <w:drawing>
                        <wp:anchor>
                            <wp:docPr id="1" name="Picture 1" descr="{{{Attribute Tag}}}"/>
                        </wp:anchor>
                    </w:drawing>
                </w:r>
                <w:r>
                    <w:t>{{tag}</w:t>
                </w:r>
                <w:r>
                    <w:t>}</w:t>
                </w:r>
                <w:r>
                    <w:t>}</w:t>
                </w:r>
            </w:p>
            """);
        var firstTextNode = TextAt(paragraph, 0, 0);

        var delimiters = CreateSearcher("{{{", "}}}").FindDelimiters(paragraph);

        Assert.Equal(4, delimiters.Count);
        Assert.IsType<AttributeDelimiterMark>(delimiters[0]);
        Assert.IsType<AttributeDelimiterMark>(delimiters[1]);
        AssertTextDelimiter(delimiters[2], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[3], false, 6, firstTextNode);
        Assert.Equal("{{{tag}}}", firstTextNode.Text);
    }

    //
    // text contains multiple delimiter prefixes
    //

    [Fact]
    public void TextContainsMultipleDelimiterPrefixes_SimpleParagraph()
    {
        var paragraph = new Paragraph(new Run(new Text("{{!#loop!}}{{!/loop!}}")));
        var textNode = TextAt(paragraph, 0, 0);

        var delimiters = CreateSearcher("{!", "!}").FindDelimiters(paragraph);

        Assert.Equal(4, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 1, textNode);
        AssertTextDelimiter(delimiters[1], false, 8, textNode);
        AssertTextDelimiter(delimiters[2], true, 12, textNode);
        AssertTextDelimiter(delimiters[3], false, 19, textNode);
    }

    [Fact]
    public void TextContainsMultipleDelimiterPrefixes_TwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(new Run(new Text("{!#lo"), new Text("op!}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var secondTextNode = TextAt(paragraph, 0, 1);

        var delimiters = CreateSearcher("{!", "!}").FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 2, secondTextNode);
    }

    [Fact]
    public void TextContainsMultipleDelimiterPrefixes_TwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{!")),
            new Run(new Text("tag")),
            new Run(new Text("!}")));
        var firstTextNode = TextAt(paragraph, 0, 0);
        var thirdTextNode = TextAt(paragraph, 2, 0);

        var delimiters = CreateSearcher("{!", "!}").FindDelimiters(paragraph);

        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, firstTextNode);
        AssertTextDelimiter(delimiters[1], false, 0, thirdTextNode);
    }

    //
    // misc
    //

    [Fact]
    public void ThrowsOnMaxXmlDepth()
    {
        var body = new Body(new Paragraph(new Run(new Text("{tag}"))));
        var searcher = new DelimiterSearcher(new Delimiters(), 2);

        Assert.Throws<Errors.MaxXmlDepthException>(() => searcher.FindDelimiters(body));
    }

    [Fact]
    public void MatchStateIsResetBetweenParagraphs()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("{{"))),
            new Paragraph(new Run(new Text("{{{tag}}}"))));
        var firstTextNode = TextAt(body, 0, 0, 0);
        var secondTextNode = TextAt(body, 1, 0, 0);

        var delimiters = CreateSearcher("{{{", "}}}").FindDelimiters(body);

        // the first paragraph's partial match is discarded (and not joined with the second paragraph)
        Assert.Equal(2, delimiters.Count);
        AssertTextDelimiter(delimiters[0], true, 0, secondTextNode);
        AssertTextDelimiter(delimiters[1], false, 6, secondTextNode);
        Assert.Equal("{{", firstTextNode.Text);
    }
}
