using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Office;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Unit.Office;

public class OfficeMarkupTests
{
    [Fact]
    public void JoinTextNodesRange_JoinsRangeOfTextNodesFromSameRun()
    {
        var paragraph = new Paragraph(new Run(new Text("1"), new Text("2"), new Text("3")));
        var run = paragraph.ChildElements[0];
        var first = TextAt(paragraph, 0, 0);
        var last = TextAt(paragraph, 0, 2);

        OfficeMarkup.Modify.JoinTextNodesRange(first, last);

        Assert.Equal(1, run.ChildElements.Count);
        Assert.Same(first, run.ChildElements[0]);
        Assert.Equal("123", first.Text);
    }

    [Fact]
    public void JoinTextNodesRange_JoinsRangeOfTextNodesFromThreeDifferentRuns()
    {
        var paragraph = new Paragraph(
            new Run(new Text("1"), new Text("2"), new Text("3")),
            new Run(new Text("4")),
            new Run(new Text("5"), new Text("6")));
        var firstRun = paragraph.ChildElements[0];
        var first = TextAt(paragraph, 0, 0);
        var last = TextAt(paragraph, 2, 1);

        OfficeMarkup.Modify.JoinTextNodesRange(first, last);

        Assert.Equal(1, paragraph.ChildElements.Count);
        Assert.Equal(1, firstRun.ChildElements.Count);
        Assert.Same(first, firstRun.ChildElements[0]);
        Assert.Equal("123456", first.Text);
    }

    [Fact]
    public void JoinTextNodesRange_DoesNotJoinNodesFromOutsideTheSpecifiedRange()
    {
        var paragraph = new Paragraph(
            new Run(new Text("0")),
            new Run(new Text("1"), new Text("2"), new Text("3")),
            new Run(new Text("4")),
            new Run(new Text("5"), new Text("6")));
        var first = TextAt(paragraph, 1, 1);
        var last = TextAt(paragraph, 2, 0);

        OfficeMarkup.Modify.JoinTextNodesRange(first, last);

        Assert.Equal(3, paragraph.ChildElements.Count);
        Assert.Equal("0", paragraph.ChildElements[0].InnerText);
        Assert.Equal("1234", paragraph.ChildElements[1].InnerText);
        Assert.Equal(2, paragraph.ChildElements[1].ChildElements.Count);
        Assert.Equal("234", paragraph.ChildElements[1].ChildElements[1].InnerText);
        Assert.Equal("56", paragraph.ChildElements[2].InnerText);
    }

    [Fact]
    public void JoinTextNodesRange_SkipsNonTextNodesInBetween()
    {
        var paragraph = new Paragraph(
            new Run(new Text("{"), new OpenXmlMiscNode(System.Xml.XmlNodeType.Comment, "<!-- comment -->"), new Text("tag}")));
        var first = TextAt(paragraph, 0, 0);
        var last = (Text)paragraph.ChildElements[0].ChildElements[2];

        OfficeMarkup.Modify.JoinTextNodesRange(first, last);

        Assert.Equal("{tag}", first.Text);
        Assert.Equal(2, paragraph.ChildElements[0].ChildElements.Count);
    }

    [Fact]
    public void JoinTextNodesRange_ThrowsForDifferentParagraphs()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("1"))),
            new Paragraph(new Run(new Text("2"))));

        Assert.Throws<ArgumentException>(() => OfficeMarkup.Modify.JoinTextNodesRange(TextAt(body, 0, 0, 0), TextAt(body, 1, 0, 0)));
    }

    [Fact]
    public void SplitTextNode_AddAfter()
    {
        var paragraph = new Paragraph(new Run(new Text("this text is split")));
        var textNode = TextAt(paragraph, 0, 0);

        var result = OfficeMarkup.Modify.SplitTextNode(textNode, 5, false);

        Assert.Equal("this ", textNode.Text);
        Assert.Equal("text is split", result.Text);
        Assert.Same(textNode, paragraph.ChildElements[0].ChildElements[0]);
        Assert.Same(result, paragraph.ChildElements[0].ChildElements[1]);
        Assert.Equal(2, paragraph.ChildElements[0].ChildElements.Count);
        Assert.Equal(SpaceProcessingModeValues.Preserve, textNode.Space?.Value);
        Assert.Equal(SpaceProcessingModeValues.Preserve, result.Space?.Value);
    }

    [Fact]
    public void SplitTextNode_AddBefore()
    {
        var paragraph = new Paragraph(new Run(new Text("this text is split")));
        var textNode = TextAt(paragraph, 0, 0);

        var result = OfficeMarkup.Modify.SplitTextNode(textNode, 5, true);

        Assert.Equal("this ", result.Text);
        Assert.Equal("text is split", textNode.Text);
        Assert.Same(result, paragraph.ChildElements[0].ChildElements[0]);
        Assert.Same(textNode, paragraph.ChildElements[0].ChildElements[1]);
        Assert.Equal(2, paragraph.ChildElements[0].ChildElements.Count);
    }

    [Fact]
    public void SplitParagraphByTextNode_PreservesTextNode_WhenRemoveTextNodeIsFalse()
    {
        var body = new Body(new Paragraph(new Run(new Text("Hello, "), new Text("world!"), new Text(" How are you?"))));
        var textNode = body.Descendants<Text>().ElementAt(1);
        var paragraph = body.Descendants<Paragraph>().First();

        var (left, right) = OfficeMarkup.Modify.SplitParagraphByTextNode(paragraph, textNode, false);

        Assert.Equal("Hello, ", left.InnerText);
        Assert.Same(textNode, right.Descendants<Text>().First());
        Assert.Equal(" How are you?", right.Descendants<Text>().ElementAt(1).Text);
        Assert.Same(paragraph, right);
        Assert.Equal(new[] { left, right }, body.ChildElements.ToArray());
    }

    [Fact]
    public void SplitParagraphByTextNode_RemovesTextNode_WhenRemoveTextNodeIsTrue()
    {
        var body = new Body(new Paragraph(new Run(new Text("Hello, "), new Text("world!"), new Text(" How are you?"))));
        var textNode = body.Descendants<Text>().ElementAt(1);
        var paragraph = body.Descendants<Paragraph>().First();

        var (left, right) = OfficeMarkup.Modify.SplitParagraphByTextNode(paragraph, textNode, true);

        Assert.Equal("Hello, ", left.InnerText);
        Assert.Equal(" How are you?", right.InnerText);
    }

    [Fact]
    public void SplitParagraphByTextNode_PreservesParagraphAndRunProperties()
    {
        var body = new Body(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(new Bold()),
                new Text("before"), new Text("{tag}"), new Text("after"))));
        var textNode = body.Descendants<Text>().ElementAt(1);
        var paragraph = body.Descendants<Paragraph>().First();

        var (left, right) = OfficeMarkup.Modify.SplitParagraphByTextNode(paragraph, textNode, true);

        Assert.NotNull(left.ParagraphProperties?.Justification);
        Assert.NotNull(right.ParagraphProperties?.Justification);
        Assert.NotNull(left.Elements<Run>().Single().RunProperties?.Bold);
        Assert.NotNull(right.Elements<Run>().Single().RunProperties?.Bold);
        Assert.Equal("before", left.InnerText);
        Assert.Equal("after", right.InnerText);
    }

    [Fact]
    public void JoinParagraphs_MovesRunsOnly()
    {
        var first = new Paragraph(new ParagraphProperties(), new Run(new Text("1")));
        var second = new Paragraph(new ParagraphProperties(), new Run(new Text("2")), new BookmarkStart(), new Run(new Text("3")));

        OfficeMarkup.Modify.JoinParagraphs(first, second);

        Assert.Equal("123", first.InnerText);
        Assert.Equal(4, first.ChildElements.Count);
        Assert.Equal(2, second.ChildElements.Count);
        Assert.IsType<BookmarkStart>(second.ChildElements[1]);
    }

    [Fact]
    public void RemoveTag_RemovesTextNodeAndEmptyRun()
    {
        var paragraph = new Paragraph(
            new Run(new Text("keep")),
            new Run(new RunProperties(new Bold()), new Text("{tag}")));
        var tag = new TextNodeTag { XmlTextNode = TextAt(paragraph, 1, 1), RawText = "{tag}" };

        OfficeMarkup.Modify.RemoveTag(tag);

        Assert.Single(paragraph.ChildElements);
        Assert.Equal("keep", paragraph.InnerText);
    }

    [Fact]
    public void RemoveTag_RemovesTagFromAttribute()
    {
        var docPr = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture 1", Description = "Hello {tag}" };
        var tag = new AttributeTag { XmlNode = docPr, AttributeName = "descr", RawText = "{tag}" };

        OfficeMarkup.Modify.RemoveTag(tag);
        Assert.Equal("Hello ", docPr.Description?.Value);

        docPr.Description = "{tag}";
        OfficeMarkup.Modify.RemoveTag(tag);
        Assert.Null(docPr.Description);
    }

    [Fact]
    public void Query_IsListParagraph()
    {
        var plain = new Paragraph(new Run(new Text("x")));
        var list = new Paragraph(new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 1 })), new Run(new Text("x")));

        Assert.False(OfficeMarkup.Query.IsListParagraph(plain));
        Assert.True(OfficeMarkup.Query.IsListParagraph(list));
    }

    [Fact]
    public void Query_ContainingNodes()
    {
        var table = ParseXml<Table>("""
            <w:tbl><w:tr><w:tc><w:p><w:r><w:t>x</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
            """);
        var text = table.Descendants<Text>().Single();

        Assert.IsType<Run>(OfficeMarkup.Query.ContainingRunNode(text));
        Assert.IsType<Paragraph>(OfficeMarkup.Query.ContainingParagraphNode(text));
        Assert.IsType<TableCell>(OfficeMarkup.Query.ContainingTableCellNode(text));
        Assert.IsType<TableRow>(OfficeMarkup.Query.ContainingTableRowNode(text));
        Assert.Same(table, OfficeMarkup.Query.ContainingTableNode(text));
        Assert.Null(OfficeMarkup.Query.ContainingStructuredTagContentNode(text));
        Assert.Null(OfficeMarkup.Query.ContainingTableNode(new Paragraph()));
    }
}
