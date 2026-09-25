using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

/// <summary>
/// Repeats table columns.
/// </summary>
public class LoopTableColumnsStrategy : ILoopStrategy
{
    public bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition)
    {
        var openCell = OfficeMarkup.Query.ContainingTableCellNode(openTag.XmlTextNode);
        if (openCell is null)
            return false;

        var closeCell = OfficeMarkup.Query.ContainingTableCellNode(closeTag.XmlTextNode);
        if (closeCell is null)
            return false;

        var forceColumnLoop = LoopTagOptions.GetLoopOver(openTag) == LoopOver.Column;

        // if both tags are in the same cell, assume it's a paragraph loop (iterate content, not columns)
        if (!forceColumnLoop && openCell == closeCell)
            return false;

        var openTable = OfficeMarkup.Query.ContainingTableNode(openCell);
        if (openTable is null)
            return false;

        var closeTable = OfficeMarkup.Query.ContainingTableNode(closeCell);
        if (closeTable is null)
            return false;

        // if the tags are in different tables, don't apply this strategy
        if (openTable != closeTable)
            return false;

        var openRow = OfficeMarkup.Query.ContainingTableRowNode(openCell);
        if (openRow is null)
            return false;

        var closeRow = OfficeMarkup.Query.ContainingTableRowNode(closeCell);
        if (closeRow is null)
            return false;

        var openColumnIndex = GetColumnIndex(openRow, openCell);
        if (openColumnIndex == -1)
            return false;

        var closeColumnIndex = GetColumnIndex(closeRow, closeCell);
        if (closeColumnIndex == -1)
            return false;

        // if the tags are in different columns, assume it's a table rows loop (iterate rows, not columns)
        if (!forceColumnLoop && openColumnIndex != closeColumnIndex)
            return false;

        return true;
    }

    public SplitBeforeResult SplitBefore(TextNodeTag openTag, TextNodeTag closeTag)
    {
        var firstCell = OfficeMarkup.Query.ContainingTableCellNode(openTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {openTag.RawText} is not inside a table cell.");
        var lastCell = OfficeMarkup.Query.ContainingTableCellNode(closeTag.XmlTextNode)
            ?? throw new TemplateSyntaxException($"Tag {closeTag.RawText} is not inside a table cell.");
        var firstRow = OfficeMarkup.Query.ContainingTableRowNode(firstCell)!;
        var lastRow = OfficeMarkup.Query.ContainingTableRowNode(lastCell)!;
        var firstColumnIndex = GetColumnIndex(firstRow, firstCell);
        var lastColumnIndex = GetColumnIndex(lastRow, lastCell);
        var table = OfficeMarkup.Query.ContainingTableNode(firstCell)!;

        // remove the loop tags
        XmlNodes.Remove(openTag.XmlTextNode);
        XmlNodes.Remove(closeTag.XmlTextNode);

        // extract the columns to repeat
        // (this is a single synthetic table with the columns to repeat)
        var columnsWrapper = ExtractColumns(table, firstColumnIndex, lastColumnIndex);

        return new SplitBeforeResult
        {
            FirstNode = firstCell,
            NodesToRepeat = new List<OpenXmlElement> { columnsWrapper },
            LastNode = lastCell
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> columnsWrapperGroups, OpenXmlElement firstNode, OpenXmlElement lastNode)
    {
        var firstCell = (TableCell)firstNode;
        var lastCell = (TableCell)lastNode;
        var table = OfficeMarkup.Query.ContainingTableNode(firstCell)!;
        var firstRow = OfficeMarkup.Query.ContainingTableRowNode(firstCell)!;
        var firstColumnIndex = GetColumnIndex(firstRow, firstCell);
        var lastRow = OfficeMarkup.Query.ContainingTableRowNode(lastCell)!;
        var lastColumnIndex = GetColumnIndex(lastRow, lastCell);

        var index = firstColumnIndex;
        foreach (var colWrapperGroup in columnsWrapperGroups)
        {
            if (colWrapperGroup.Count != 1)
                throw new InternalException("Expected a single synthetic table as the columns wrapper.");

            var colWrapper = colWrapperGroup[0];
            InsertColumnAfterIndex(table, colWrapper, index);
            index++;
        }

        // remove the old columns
        RemoveColumn(table, firstColumnIndex);
        if (firstColumnIndex != lastColumnIndex)
            RemoveColumn(table, lastColumnIndex + index);
    }

    private static Table ExtractColumns(Table table, int firstColumnIndex, int lastColumnIndex)
    {
        // create a synthetic table to hold the columns
        var syntheticTable = new Table();

        // for each row in the original table
        foreach (var row in table.Elements<TableRow>())
        {
            var syntheticRow = (TableRow)row.CloneNode(false);
            var cells = row.Elements<TableCell>().ToList();

            // copy only the cells within our column range
            for (var i = firstColumnIndex; i <= lastColumnIndex; i++)
            {
                if (i < cells.Count)
                    syntheticRow.AppendChild(cells[i].CloneNode(true));
            }

            syntheticTable.AppendChild(syntheticRow);
        }

        return syntheticTable;
    }

    private static void InsertColumnAfterIndex(Table table, OpenXmlElement column, int index)
    {
        // get all rows from both tables
        var sourceRows = column.Elements<TableRow>().ToList();
        var targetRows = table.Elements<TableRow>().ToList();

        // insert columns in the target table
        for (var i = 0; i < targetRows.Count; i++)
        {
            var targetRow = targetRows[i];
            if (i >= sourceRows.Count)
                continue;
            var sourceRow = sourceRows[i];

            // we expect exactly one cell per row in the synthetic source table
            var sourceCell = sourceRow.Elements<TableCell>().FirstOrDefault()
                ?? throw new InternalException($"Cell not found in synthetic source table row {i}.");

            var targetCell = GetColumnByIndex(targetRow, index);
            var newCell = sourceCell.CloneNode(true);
            if (targetCell != null)
                targetCell.InsertAfterSelf(newCell);
            else
                targetRow.AppendChild(newCell);
        }
    }

    private static void RemoveColumn(Table table, int index)
    {
        foreach (var row in table.Elements<TableRow>().ToList())
        {
            var cell = GetColumnByIndex(row, index);
            cell?.Remove();
        }
    }

    private static int GetColumnIndex(TableRow row, TableCell cell)
    {
        var index = 0;
        foreach (var child in row.Elements<TableCell>())
        {
            if (child == cell)
                return index;
            index++;
        }
        return -1;
    }

    private static TableCell? GetColumnByIndex(TableRow row, int index)
    {
        return row.Elements<TableCell>().ElementAtOrDefault(index);
    }
}
