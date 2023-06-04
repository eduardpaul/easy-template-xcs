using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Compilation;

public class MatchState
{
    public int DelimiterIndex = 0;
    public List<Text> OpenNodes = new List<Text>();
    public int FirstMatchIndex = -1;

    public void Reset()
    {
        DelimiterIndex = 0;
        OpenNodes.Clear();
        FirstMatchIndex = -1;
    }
}

public class DelimiterSearcher
{
    public int MaxXmlDepth = 20;
    public string StartDelimiter = "{";
    public string EndDelimiter = "}";

    public DelimiterSearcher() { }

    public List<DelimiterMark> FindDelimiters(OpenXmlElement node)
    {
        //
        // Performance note:
        //
        // The search efficiency is o(m*n) where n is the text size and m is the
        // delimiter length. We could use a variation of the KMP algorithm here
        // to reduce it to o(m+n) but since our m is expected to be small
        // (delimiters defaults to 2 characters and even on custom inputs are
        // not expected to be much longer) it does not worth the extra
        // complexity and effort.
        //

        var delimiters = new List<DelimiterMark>();
        var match = new MatchState();
        var depth = new XmlDepthTracker(MaxXmlDepth);
        var lookForOpenDelimiter = true;

        while (node != null)
        {
            // reset state on paragraph transition
            if (node is Paragraph)
                match.Reset();

            // skip irrelevant nodes
            if (!ShouldSearchNode(node))
            {
                node = FindNextNode(node, depth);
                continue;
            }

            // search delimiters in text nodes
            match.OpenNodes.Add((Text)node);
            var textIndex = 0;
            while (textIndex < node.InnerText.Length)
            {
                var delimiterPattern = lookForOpenDelimiter ? StartDelimiter : EndDelimiter;
                var character = node.InnerText[textIndex];

                // no match
                if (character != delimiterPattern[match.DelimiterIndex])
                {
                    (node, textIndex) = NoMatch((Text)node, textIndex, match);
                    textIndex++;
                    continue;
                }

                // first match
                if (match.FirstMatchIndex == -1)
                {
                    match.FirstMatchIndex = textIndex;
                }

                // partial match
                if (match.DelimiterIndex != delimiterPattern.Length - 1)
                {
                    match.DelimiterIndex++;
                    textIndex++;
                    continue;
                }

                // full delimiter match
                (node, textIndex, lookForOpenDelimiter) = FullMatch((Text)node, textIndex, lookForOpenDelimiter, match, delimiters);
                textIndex++;
            }

            node = FindNextNode(node, depth);
        }

        return delimiters;
    }

    private (Text, int) NoMatch(Text node, int textIndex, MatchState match)
    {
        //
        // go back to first open node
        //
        // Required for cases where the text has repeating
        // characters that are the same as a delimiter prefix.
        // For instance:
        // Delimiter is '{!' and template text contains the string '{{!'
        //
        if (match.FirstMatchIndex != -1)
        {
            node = match.OpenNodes.First();
            textIndex = match.FirstMatchIndex;
        }

        // update state
        match.Reset();
        if (textIndex < node.InnerText.Length - 1)
        {
            match.OpenNodes.Add((Text)node);
        }

        return (node, textIndex);
    }

    private (Text, int, bool) FullMatch(Text node, int textIndex, bool lookForOpenDelimiter, MatchState match, List<DelimiterMark> delimiters)
    {
        // move all delimiters characters to the same text node
        if (match.OpenNodes.Count > 1)
        {
            var firstNode = match.OpenNodes.First();
            var lastNode = match.OpenNodes.Last();
            DocParserHelpers.JoinTextNodesRange(firstNode, lastNode);

            textIndex += (firstNode.InnerText.Length - node.InnerText.Length);
            node = firstNode;
        }

        // store delimiter
        var delimiterMark = CreateDelimiterMark(match, lookForOpenDelimiter);
        delimiters.Add(delimiterMark);

        // update state
        lookForOpenDelimiter = !lookForOpenDelimiter;
        match.Reset();
        if (textIndex < node.InnerText.Length - 1)
        {
            match.OpenNodes.Add((Text)node);
        }

        return (node, textIndex, lookForOpenDelimiter);
    }

    private bool ShouldSearchNode(OpenXmlElement node)
    {
        if (!(node is Text))
            return false;
        if (string.IsNullOrEmpty(node.InnerText))
            return false;
        if (node.Parent == null)
            return false;
        //if (!(node.Parent is Text)) // verify if this is needed
        //   return false;

        return true;
    }

    private OpenXmlElement? FindNextNode(OpenXmlElement node, XmlDepthTracker depth)
    {
        // children
        if (node.HasChildren)
        {
            depth.Increment();
            return node?.FirstChild;
        }

        // siblings
        if (node.NextSibling() != null)
            return node.NextSibling();

        // parent sibling
        while (node.Parent != null)
        {
            if (node.Parent.NextSibling() != null)
            {
                depth.Decrement();
                return node.Parent.NextSibling();
            }

            // go up
            depth.Decrement();
            node = node.Parent;
        }

        return null;
    }

    private DelimiterMark CreateDelimiterMark(MatchState match, bool isOpenDelimiter)
    {
        return new DelimiterMark
        {
            Index = match.FirstMatchIndex,
            IsOpen = isOpenDelimiter,
            XmlTextNode = match.OpenNodes.First()
        };
    }
}