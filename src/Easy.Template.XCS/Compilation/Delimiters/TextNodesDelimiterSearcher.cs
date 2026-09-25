using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Compilation.Delimiters;

public class TextNodesDelimiterSearcher
{
    private bool lookForOpenDelimiter = true;

    /// <summary>
    /// The index of the current delimiter character being matched.
    ///
    /// Example: If the delimiter is `{!` and delimiterIndex is 0, it means we
    /// are now looking for the character `{`. If it is 1, then we are looking
    /// for `!`.
    /// </summary>
    private int lookForDelimiterIndex;

    /// <summary>
    /// The list of text nodes containing the delimiter characters of the current match.
    /// </summary>
    private List<Text> matchOpenNodes = new();

    /// <summary>
    /// The index of the first character of the current delimiter match, in the text node it
    /// was found at.
    ///
    /// Example: If the delimiter is `{!`, and the text node content is `abc{!xyz`,
    /// then the firstMatchIndex is 3.
    /// </summary>
    private int firstMatchIndex = -1;

    private readonly string startDelimiter;
    private readonly string endDelimiter;

    public TextNodesDelimiterSearcher(string startDelimiter, string endDelimiter)
    {
        this.startDelimiter = startDelimiter;
        this.endDelimiter = endDelimiter;
    }

    public void ProcessNode(XmlTreeIterator it, List<DelimiterMark> delimiters)
    {
        // reset match state on paragraph transition
        if (OfficeMarkup.Query.IsParagraphNode(it.Node))
            ResetMatch();

        // reset match state on inline drawing
        if (OfficeMarkup.Query.IsInlineDrawingNode(it.Node))
            ResetMatch();

        // ignore non-text nodes
        if (!ShouldSearchNode(it.Node, out var textNode))
            return;

        // search delimiters in text nodes
        FindDelimiters(it, textNode, delimiters);
    }

    private void ResetMatch()
    {
        lookForDelimiterIndex = 0;
        matchOpenNodes = new List<Text>();
        firstMatchIndex = -1;
    }

    private static bool ShouldSearchNode(OpenXmlElement? node, out Text textNode)
    {
        textNode = null!;
        if (node is not Text text)
            return false;
        if (string.IsNullOrEmpty(text.Text))
            return false;
        if (text.Parent is null)
            return false;

        textNode = text;
        return true;
    }

    private void FindDelimiters(XmlTreeIterator it, Text node, List<DelimiterMark> delimiters)
    {
        //
        // Performance note:
        //
        // The search efficiency is o(m*n) where n is the text size and m is the
        // delimiter length. We could use a variation of the KMP algorithm here
        // to reduce it to o(m+n) but since our m is expected to be small
        // (delimiters defaults to a single characters and even on custom inputs
        // are not expected to be much longer) it does not worth the extra
        // complexity and effort.
        //

        matchOpenNodes.Add(node);
        var textIndex = 0;
        while (textIndex < node.Text.Length)
        {
            var delimiterPattern = lookForOpenDelimiter ? startDelimiter : endDelimiter;
            var c = node.Text[textIndex];

            // no match
            if (c != delimiterPattern[lookForDelimiterIndex])
            {
                (node, textIndex) = NoMatch(it, node, textIndex);
                textIndex++;
                continue;
            }

            // first match
            if (firstMatchIndex == -1)
                firstMatchIndex = textIndex;

            // partial match
            if (lookForDelimiterIndex != delimiterPattern.Length - 1)
            {
                lookForDelimiterIndex++;
                textIndex++;
                continue;
            }

            // full delimiter match
            (node, textIndex) = FullMatch(it, node, textIndex, delimiters);
            textIndex++;
        }
    }

    private (Text Node, int TextIndex) NoMatch(XmlTreeIterator it, Text node, int textIndex)
    {
        //
        // Go back to first open node
        //
        // Required for cases where the text has repeating
        // characters that are the same as a delimiter prefix.
        // For instance:
        // Delimiter is '{!' and template text contains the string '{{!'
        //
        if (firstMatchIndex != -1)
        {
            node = matchOpenNodes[0];
            it.SetCurrent(node);
            textIndex = firstMatchIndex;
        }

        // update state
        ResetMatch();
        if (textIndex < node.Text.Length - 1)
            matchOpenNodes.Add(node);

        return (node, textIndex);
    }

    private (Text Node, int TextIndex) FullMatch(XmlTreeIterator it, Text node, int textIndex, List<DelimiterMark> delimiters)
    {
        // move all delimiters characters to the same text node
        if (matchOpenNodes.Count > 1)
        {
            var firstNode = matchOpenNodes[0];
            var lastNode = matchOpenNodes[matchOpenNodes.Count - 1];
            OfficeMarkup.Modify.JoinTextNodesRange(firstNode, lastNode);
            textIndex += firstNode.Text.Length - node.Text.Length;
            node = firstNode;
            it.SetCurrent(firstNode);
        }

        // store delimiter
        var delimiterMark = CreateCurrentDelimiterMark();
        delimiters.Add(delimiterMark);

        // update state
        lookForOpenDelimiter = !lookForOpenDelimiter;
        ResetMatch();
        if (textIndex < node.Text.Length - 1)
            matchOpenNodes.Add(node);

        return (node, textIndex);
    }

    private TextNodeDelimiterMark CreateCurrentDelimiterMark()
    {
        return new TextNodeDelimiterMark
        {
            IsOpen = lookForOpenDelimiter,
            Index = firstMatchIndex,
            XmlTextNode = matchOpenNodes[0]
        };
    }
}
