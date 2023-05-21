
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace test_easy_template_xcs;

public class DocxParserHelpersTests
{
    [Fact]
    public void JoinTextNodesRange_JoinsRangeOfTextNodesFromSameRun()
    {
        var paragraphNode = new Paragraph(
               new Run(
                   new Text("1"),
                   new Text("2"),
                   new Text("3")
               ));

        var runNode = paragraphNode.ChildElements[0];
        var firstXmlTextNode = (OpenXmlElement)runNode.ChildElements[0];
        Assert.Equal("1", firstXmlTextNode.InnerText);
        var lastXmlTextNode = (OpenXmlElement)runNode.ChildElements[2];
        Assert.Equal("3", lastXmlTextNode.InnerText);

        DocParserHelpers.JoinTextNodesRange(firstXmlTextNode as Text, lastXmlTextNode as Text);

        Assert.Equal(1, runNode.ChildElements.Count);
        Assert.Equal(1, runNode.ChildElements.Count);
        Assert.Equal(firstXmlTextNode, runNode.ChildElements[0]);
        Assert.Equal("123", firstXmlTextNode.InnerText);
    }

    [Fact]
    public void JoinTextNodesRange_JoinsRangeOfTextNodesFromThreeDifferentRuns()
    {
        var paragraphNode = new Paragraph(
         new Run(
             new Text("1"),
             new Text("2"),
             new Text("3")
         ),
         new Run(
             new Text("4")
         ),
         new Run(
             new Text("5"),
             new Text("6")
         ));

        var firstRunNode = paragraphNode.ChildElements[0];
        var firstXmlTextNode = (OpenXmlElement)firstRunNode.ChildElements[0];
        Assert.Equal("1", firstXmlTextNode.InnerText);
        var thirdRunNode = paragraphNode.ChildElements[2];
        var lastXmlTextNode = (OpenXmlElement)thirdRunNode.ChildElements[1];
        Assert.Equal("6", lastXmlTextNode.InnerText);

        DocParserHelpers.JoinTextNodesRange(firstXmlTextNode as Text, lastXmlTextNode as Text);

        Assert.Equal(1, paragraphNode.ChildElements.Count);
        Assert.Equal(1, firstRunNode.ChildElements.Count);
        Assert.Equal(firstXmlTextNode, firstRunNode.ChildElements[0]);
        Assert.Equal("123456", firstXmlTextNode.InnerText);
    }

    [Fact]
    public void JoinTextNodesRange_DoesNotJoinNodesFromOutsideTheSpecifiedRange()
    {
        var paragraphNode = new Paragraph(
            new Run(
             new Text("0")),
            new Run(
                new Text("1"),
                new Text("2"),
                new Text("3")
            ),
            new Run(
                new Text("4")
            ),
            new Run(
                new Text("5"),
                new Text("6")
            ));

        var firstRunNode = paragraphNode.ChildElements[1];
        var firstXmlTextNode = (OpenXmlElement)firstRunNode.ChildElements[1];
        Assert.Equal("2", firstXmlTextNode.InnerText);
        var thirdRunNode = paragraphNode.ChildElements[2];
        var lastXmlTextNode = (OpenXmlElement)thirdRunNode.ChildElements[0];
        Assert.Equal("4", lastXmlTextNode.InnerText);

        DocParserHelpers.JoinTextNodesRange(firstXmlTextNode as Text, lastXmlTextNode as Text);

        Assert.Equal(3, paragraphNode.ChildElements.Count);
        Assert.Equal(2, firstRunNode.ChildElements.Count);
        Assert.Equal("0", paragraphNode.ChildElements[0].InnerText);
        Assert.Equal("1234", paragraphNode.ChildElements[1].InnerText);
        Assert.Equal("234", paragraphNode.ChildElements[1].ChildElements[1].InnerText);
        Assert.Equal("56", paragraphNode.ChildElements[2].InnerText);
    }

    [Fact]
    public void SplitTextNode_SimpleTextAddAfter()
    {
        var paragrap = new Paragraph(
            new Run(
             new Text("this text is split"))
            );

        var firstTextNode = (Text)paragrap.ChildElements[0].ChildElements[0];
        var splitResult = DocParserHelpers.SplitTextNode(firstTextNode, 5, false);

        Assert.Equal("this ", firstTextNode.Text);
        Assert.Equal("text is split", splitResult.Text);
        Assert.Equal(firstTextNode, paragrap.ChildElements[0].ChildElements[0]);
        Assert.Equal(2, paragrap.ChildElements[0].ChildElements.Count);
    }

    [Fact]
    public void SplitTextNode_SimpleTextAddBefore()
    {
        var paragrap = new Paragraph(
            new Run(
             new Text("this text is split"))
            );

        var firstTextNode = (Text)paragrap.ChildElements[0].ChildElements[0];
        var splitResult = DocParserHelpers.SplitTextNode(firstTextNode, 5, true);

        Assert.Equal("this ", splitResult.Text);
        Assert.Equal("text is split", firstTextNode.Text);
        Assert.Equal(firstTextNode, paragrap.ChildElements[0].ChildElements[1]);
        Assert.Equal(2, paragrap.ChildElements[0].ChildElements.Count);
    }

    [Fact]
    public void SplitParagraphByTextNode_PreservesTextNode_WhenRemoveTextNodeIsFalse()
    {
        // Arrange
        var body = new Body(new Paragraph(
            new Run(
                new Text("Hello, "),
                new Text("world!"),
                new Text(" How are you?")
            )
        ));
        var textNode = body.Descendants<Text>().ElementAt(1);
        var paragraph = body.Descendants<Paragraph>().First();

        // Act
        var (leftPara, rightPara) = DocParserHelpers.SplitParagraphByTextNode(paragraph, textNode, false);

        // Assert
        Assert.Equal("Hello, ", leftPara.InnerText);
        Assert.Equal("world!", rightPara.Descendants<Text>().First().InnerText);
        Assert.Equal(" How are you?", rightPara.Descendants<Text>().ElementAt(1).InnerText);
    }

    [Fact]
    public void SplitParagraphByTextNode_RemovesTextNode_WhenRemoveTextNodeIsTrue()
    {
        // Arrange
        var body = new Body(new Paragraph(
            new Run(
                new Text("Hello, "),
                new Text("world!"),
                new Text(" How are you?")
            )
        ));
        var textNode = body.Descendants<Text>().ElementAt(1);
        var paragraph = body.Descendants<Paragraph>().First();

        // Act
        var (leftPara, rightPara) = DocParserHelpers.SplitParagraphByTextNode(paragraph, textNode, true);

        // Assert
        Assert.Equal("Hello, ", leftPara.InnerText);
        Assert.Equal(" How are you?", rightPara.InnerText);
    }
}