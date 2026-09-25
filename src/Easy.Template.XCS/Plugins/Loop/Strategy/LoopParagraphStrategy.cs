using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

/// <summary>
/// Repeats entire paragraphs (activated by the <c>loopOver: "paragraph"</c> tag option).
/// </summary>
public class LoopParagraphStrategy : ILoopStrategy
{
    public bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition)
    {
        return LoopTagOptions.GetLoopOver(openTag) == LoopOver.Paragraph;
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

    public void MergeBack(List<List<OpenXmlElement>> newParagraphs, OpenXmlElement firstParagraph, OpenXmlElement lastParagraph)
    {
        // add new paragraphs to the document
        var insertAfter = lastParagraph;
        foreach (var curParagraphsGroup in newParagraphs)
        {
            foreach (var paragraph in curParagraphsGroup)
            {
                XmlNodes.InsertAfter(paragraph, insertAfter);
                insertAfter = paragraph;
            }
        }

        // we cannot leave table cells completely empty, so we track them
        // see: http://officeopenxml.com/WPtableCell.php
        var firstTableCell = OfficeMarkup.Query.ContainingTableCellNode(firstParagraph);
        var lastTableCell = OfficeMarkup.Query.ContainingTableCellNode(lastParagraph);

        // remove old paragraphs - between first and last paragraph
        XmlNodes.RemoveSiblings(firstParagraph, lastParagraph);

        // remove old paragraphs - first and last
        XmlNodes.Remove(firstParagraph);
        if (firstParagraph != lastParagraph)
            XmlNodes.Remove(lastParagraph);

        // make sure the table cells are not empty (if they exist)
        if (newParagraphs.Count == 0)
        {
            FillTableCell(firstTableCell);
            FillTableCell(lastTableCell);
        }
    }

    private static void FillTableCell(TableCell? tableCell)
    {
        if (tableCell is null)
            return;

        if (tableCell.ChildElements.Any(node => node is Paragraph or Table))
            return;

        tableCell.AppendChild(new Paragraph());
    }
}
