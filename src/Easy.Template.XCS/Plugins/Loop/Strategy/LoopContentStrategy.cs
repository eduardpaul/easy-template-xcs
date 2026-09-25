using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

/// <summary>
/// Repeats the content enclosed between the opening and closing tags.
/// This is the default strategy.
/// </summary>
public class LoopContentStrategy : ILoopStrategy
{
    public bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition)
    {
        return true;
    }

    public SplitBeforeResult SplitBefore(TextNodeTag openTag, TextNodeTag closeTag)
    {
        // gather some info
        OpenXmlElement firstParagraph = OfficeMarkup.Query.ContainingParagraphNode(openTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {openTag.RawText} is not inside a paragraph.");
        OpenXmlElement lastParagraph = OfficeMarkup.Query.ContainingParagraphNode(closeTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {closeTag.RawText} is not inside a paragraph.");
        var areSame = firstParagraph == lastParagraph;

        // make sure the paragraphs are siblings
        if (firstParagraph.Parent != lastParagraph.Parent)
            throw new TemplateSyntaxException($"Open and close tags are not in the same container: {openTag.RawText} and {closeTag.RawText}. For example, one is inside a table cell and the other is not.");

        // split first paragraph
        const bool removeTextNode = true;
        var splitResult = OfficeMarkup.Modify.SplitParagraphByTextNode(firstParagraph, openTag.XmlTextNode, removeTextNode);
        firstParagraph = splitResult.Left;
        OpenXmlElement afterFirstParagraph = splitResult.Right;
        if (areSame)
            lastParagraph = afterFirstParagraph;

        // split last paragraph
        splitResult = OfficeMarkup.Modify.SplitParagraphByTextNode(lastParagraph, closeTag.XmlTextNode, removeTextNode);
        OpenXmlElement beforeLastParagraph = splitResult.Left;
        lastParagraph = splitResult.Right;
        if (areSame)
            afterFirstParagraph = beforeLastParagraph;

        // disconnect splitted paragraph from their parents
        XmlNodes.Remove(afterFirstParagraph);
        if (!areSame)
            XmlNodes.Remove(beforeLastParagraph);

        // extract all paragraphs in between
        List<OpenXmlElement> middleParagraphs;
        if (areSame)
        {
            middleParagraphs = new List<OpenXmlElement> { afterFirstParagraph };
        }
        else
        {
            var inBetween = XmlNodes.RemoveSiblings(firstParagraph, lastParagraph);
            middleParagraphs = new List<OpenXmlElement> { afterFirstParagraph };
            middleParagraphs.AddRange(inBetween);
            middleParagraphs.Add(beforeLastParagraph);
        }

        return new SplitBeforeResult
        {
            FirstNode = firstParagraph,
            NodesToRepeat = middleParagraphs,
            LastNode = lastParagraph
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> middleParagraphs, OpenXmlElement firstParagraph, OpenXmlElement lastParagraph)
    {
        // Note: after SplitBefore, 'firstParagraph' and 'lastParagraph' are
        // adjacent siblings. We always insert right after the last merged
        // paragraph (rather than before 'lastParagraph') since inserting after
        // a known node is O(1) in the OpenXml SDK while inserting before a node
        // requires a linear scan of its siblings.
        var mergeTo = firstParagraph;
        foreach (var curParagraphsGroup in middleParagraphs)
        {
            // merge first paragraphs
            OfficeMarkup.Modify.JoinParagraphs(mergeTo, curParagraphsGroup[0]);

            // add middle and last paragraphs to the original document
            for (var i = 1; i < curParagraphsGroup.Count; i++)
            {
                XmlNodes.InsertAfter(curParagraphsGroup[i], mergeTo);
                mergeTo = curParagraphsGroup[i];
            }
        }

        // merge last paragraph
        OfficeMarkup.Modify.JoinParagraphs(mergeTo, lastParagraph);

        // remove the old last paragraph (was merged into the new one)
        XmlNodes.Remove(lastParagraph);
    }
}
