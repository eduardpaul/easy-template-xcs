using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

/// <summary>
/// Repeats table rows.
/// </summary>
public class LoopTableRowsStrategy : ILoopStrategy
{
    public bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition)
    {
        var openCell = OfficeMarkup.Query.ContainingTableCellNode(openTag.XmlTextNode);
        if (openCell is null)
            return false;

        var closeCell = OfficeMarkup.Query.ContainingTableCellNode(closeTag.XmlTextNode);
        if (closeCell is null)
            return false;

        var forceRowLoop = LoopTagOptions.GetLoopOver(openTag) == LoopOver.Row;

        // if both tags are in the same cell, assume it's a paragraph loop (iterate content, not rows)
        if (!forceRowLoop && openCell == closeCell)
            return false;

        return true;
    }

    public SplitBeforeResult SplitBefore(TextNodeTag openTag, TextNodeTag closeTag)
    {
        var firstRow = OfficeMarkup.Query.ContainingTableRowNode(openTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {openTag.RawText} is not inside a table row.");
        var lastRow = OfficeMarkup.Query.ContainingTableRowNode(closeTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {closeTag.RawText} is not inside a table row.");

        var firstTable = OfficeMarkup.Query.ContainingTableNode(firstRow);
        var lastTable = OfficeMarkup.Query.ContainingTableNode(lastRow);
        if (firstTable != lastTable)
            throw new TemplateSyntaxException($"Open and close tags are not in the same table: {openTag.RawText} and {closeTag.RawText}. Are you trying to repeat rows across adjacent or nested tables?");

        var rowsToRepeat = XmlNodes.SiblingsInRange(firstRow, lastRow);

        // remove the loop tags
        XmlNodes.Remove(openTag.XmlTextNode);
        XmlNodes.Remove(closeTag.XmlTextNode);

        return new SplitBeforeResult
        {
            FirstNode = firstRow,
            NodesToRepeat = rowsToRepeat,
            LastNode = lastRow
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> rowGroups, OpenXmlElement firstRow, OpenXmlElement lastRow)
    {
        var insertAfter = lastRow;
        foreach (var curRowsGroup in rowGroups)
        {
            foreach (var row in curRowsGroup)
            {
                XmlNodes.InsertAfter(row, insertAfter);
                insertAfter = row;
            }
        }

        // remove old rows - between first and last row
        XmlNodes.RemoveSiblings(firstRow, lastRow);

        // remove old rows - first and last rows
        XmlNodes.Remove(firstRow);
        if (firstRow != lastRow)
            XmlNodes.Remove(lastRow);
    }
}
