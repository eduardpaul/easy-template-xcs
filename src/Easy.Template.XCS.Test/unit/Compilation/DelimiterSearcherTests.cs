

using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;

namespace test_easy_template_xcs;

public class DelimiterSearcherTests
{
    [Fact]
    public void SingleCharacterDelimiters_FindsAllDelimitersInASimpleParagraph()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{#loop}{/loop}")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var expected = new[]
        {
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 6,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = true,
                Index = 7,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 13,
                XmlTextNode = textNode
            }
        };

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{";
        searcher.EndDelimiter = "}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);
        Assert.Equal(expected[2].Index, delimiters[2].Index);
        Assert.Equal(expected[3].Index, delimiters[3].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);
        Assert.Equal(expected[2].IsOpen, delimiters[2].IsOpen);
        Assert.Equal(expected[3].IsOpen, delimiters[3].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
        Assert.Equal(expected[2].XmlTextNode, delimiters[2].XmlTextNode);
        Assert.Equal(expected[3].XmlTextNode, delimiters[3].XmlTextNode);
    }

    [Fact]
    public void SingleCharacterDelimiters_FindsAllDelimitersInTwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{#lo"),
                new Text("op}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[0].ChildElements[1];
        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 2,
                XmlTextNode = secondTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{";
        searcher.EndDelimiter = "}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }

    [Fact]
    public void SingleCharacterDelimiters_FindsAllDelimitersInTwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{")
            ),
            new Run(
                new Text("tag")
            ),
            new Run(
                new Text("}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var thirdTextNode = (Text)paragraph.ChildElements[2].ChildElements[0];

        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 0,
                XmlTextNode = thirdTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{";
        searcher.EndDelimiter = "}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_FindsAllDelimitersInASimpleParagraph()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{{#loop}}{{/loop}}")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var expected = new[]
        {
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 7,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = true,
                Index = 9,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 16,
                XmlTextNode = textNode
            }
        };

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{{";
        searcher.EndDelimiter = "}}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);
        Assert.Equal(expected[2].Index, delimiters[2].Index);
        Assert.Equal(expected[3].Index, delimiters[3].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);
        Assert.Equal(expected[2].IsOpen, delimiters[2].IsOpen);
        Assert.Equal(expected[3].IsOpen, delimiters[3].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
        Assert.Equal(expected[2].XmlTextNode, delimiters[2].XmlTextNode);
        Assert.Equal(expected[3].XmlTextNode, delimiters[3].XmlTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_FindsAllDelimitersInTwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{{#lo"),
                new Text("op}}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[0].ChildElements[1];
        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 2,
                XmlTextNode = secondTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{{";
        searcher.EndDelimiter = "}}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_FindsAllDelimitersInTwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{{")
            ),
            new Run(
                new Text("tag")
            ),
            new Run(
                new Text("}}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var thirdTextNode = (Text)paragraph.ChildElements[2].ChildElements[0];

        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 0,
                XmlTextNode = thirdTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{{";
        searcher.EndDelimiter = "}}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }

    [Fact]
    public void MultiCharacterDelimiters_HandlesDelimitersSplittedToSeveralDifferentRunNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{")
            ),
            new Run(
                new Text("{{tag}")
            ),
            new Run(
                new Text("}")
            ),
            new Run(
                new Text("}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 6,
                XmlTextNode = firstTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{{{";
        searcher.EndDelimiter = "}}}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }

    [Fact]
    public void TextContainMultipleDelimitersPrefixes_FindsAllDelimitersInASimpleParagraph()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{{!#loop!}}{{!/loop!}}")
            ));

        var textNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var expected = new[]
        {
            new DelimiterMark
            {
                IsOpen = true,
                Index = 1,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 8,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = true,
                Index = 12,
                XmlTextNode = textNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 19,
                XmlTextNode = textNode
            }
        };

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{!";
        searcher.EndDelimiter = "!}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);
        Assert.Equal(expected[2].Index, delimiters[2].Index);
        Assert.Equal(expected[3].Index, delimiters[3].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);
        Assert.Equal(expected[2].IsOpen, delimiters[2].IsOpen);
        Assert.Equal(expected[3].IsOpen, delimiters[3].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
        Assert.Equal(expected[2].XmlTextNode, delimiters[2].XmlTextNode);
        Assert.Equal(expected[3].XmlTextNode, delimiters[3].XmlTextNode);
    }

    [Fact]
    public void TextContainMultipleDelimitersPrefixes_FindsAllDelimitersInTwoDifferentTextNodes()
    {
        var paragraph = new Paragraph(
            new Run(
                new Text("{!#lo"),
                new Text("op!}")
            ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var secondTextNode = (Text)paragraph.ChildElements[0].ChildElements[1];
        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 2,
                XmlTextNode = secondTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{!";
        searcher.EndDelimiter = "!}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }
    
    [Fact]
    public void TextContainMultipleDelimitersPrefixes_FindsAllDelimitersInTwoDifferentRunNodes()
    {
        var paragraph = new Paragraph(
                    new Run(
                        new Text("{!")
                    ),
                    new Run(
                        new Text("tag")
                    ),
                    new Run(
                        new Text("!}")
                    ));

        var firstTextNode = (Text)paragraph.ChildElements[0].ChildElements[0];
        var thirdTextNode = (Text)paragraph.ChildElements[2].ChildElements[0];

        var expected = new[]{
            new DelimiterMark
            {
                IsOpen = true,
                Index = 0,
                XmlTextNode = firstTextNode
            },
            new DelimiterMark
            {
                IsOpen = false,
                Index = 0,
                XmlTextNode = thirdTextNode
            }};

        var searcher = new DelimiterSearcher();
        searcher.StartDelimiter = "{!";
        searcher.EndDelimiter = "!}";
        var delimiters = searcher.FindDelimiters(paragraph);

        Assert.Equal(expected[0].Index, delimiters[0].Index);
        Assert.Equal(expected[1].Index, delimiters[1].Index);

        Assert.Equal(expected[0].IsOpen, delimiters[0].IsOpen);
        Assert.Equal(expected[1].IsOpen, delimiters[1].IsOpen);

        Assert.Equal(expected[0].XmlTextNode, delimiters[0].XmlTextNode);
        Assert.Equal(expected[1].XmlTextNode, delimiters[1].XmlTextNode);
    }
}