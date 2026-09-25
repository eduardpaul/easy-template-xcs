using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

/// <summary>
/// Repeats list items (numbered or bulleted paragraphs).
/// </summary>
public class LoopListStrategy : ILoopStrategy
{
    public bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition)
    {
        if (isCondition)
            return false;

        var containingParagraph = OfficeMarkup.Query.ContainingParagraphNode(openTag.XmlTextNode);
        return containingParagraph != null && OfficeMarkup.Query.IsListParagraph(containingParagraph);
    }

    public SplitBeforeResult SplitBefore(TextNodeTag openTag, TextNodeTag closeTag)
    {
        var firstParagraph = OfficeMarkup.Query.ContainingParagraphNode(openTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {openTag.RawText} is not inside a paragraph.");
        var lastParagraph = OfficeMarkup.Query.ContainingParagraphNode(closeTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {closeTag.RawText} is not inside a paragraph.");

        // make sure the paragraphs are siblings
        if (firstParagraph.Parent != lastParagraph.Parent)
            throw new TemplateSyntaxException($"Open and close tags are not in the same container: {openTag.RawText} and {closeTag.RawText}. For example, one is inside a table cell and the other is not.");

        var paragraphsToRepeat = XmlNodes.SiblingsInRange(firstParagraph, lastParagraph);

        // remove the loop tags
        XmlNodes.Remove(openTag.XmlTextNode);
        XmlNodes.Remove(closeTag.XmlTextNode);

        return new SplitBeforeResult
        {
            FirstNode = firstParagraph,
            NodesToRepeat = paragraphsToRepeat,
            LastNode = lastParagraph
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> paragraphGroups, OpenXmlElement firstParagraph, OpenXmlElement lastParagraph)
    {
        // add new paragraphs to the document
        var insertAfter = lastParagraph;
        foreach (var curParagraphsGroup in paragraphGroups)
        {
            foreach (var paragraph in curParagraphsGroup)
            {
                XmlNodes.InsertAfter(paragraph, insertAfter);
                insertAfter = paragraph;
            }
        }

        // remove old paragraphs - between first and last paragraph
        XmlNodes.RemoveSiblings(firstParagraph, lastParagraph);

        // remove old paragraphs - first and last
        XmlNodes.Remove(firstParagraph);
        if (firstParagraph != lastParagraph)
            XmlNodes.Remove(lastParagraph);
    }
}
