using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation.Delimiters;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Utils;
using Easy.Template.XCS.Xml;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using WpInline = DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline;

namespace Easy.Template.XCS.Compilation;

public class TagParser
{
    private readonly Regex tagRegex;
    private readonly XCS.Delimiters delimiters;

    public TagParser(XCS.Delimiters delimiters)
    {
        if (delimiters is null)
            throw new InternalArgumentMissingException(nameof(delimiters));

        this.delimiters = delimiters;
        tagRegex = TagUtils.TagRegex(delimiters);
    }

    public List<Tag> Parse(IReadOnlyList<DelimiterMark> delimiterMarks)
    {
        var tags = new List<Tag>();

        DelimiterMark? openedTextDelimiter = null;
        DelimiterMark? openedAttributeDelimiter = null;

        for (var i = 0; i < delimiterMarks.Count; i++)
        {
            switch (delimiterMarks[i].Placement)
            {
                case TagPlacement.TextNode:
                    openedTextDelimiter = ProcessDelimiter(delimiterMarks, i, openedTextDelimiter, tags);
                    break;
                case TagPlacement.Attribute:
                    openedAttributeDelimiter = ProcessDelimiter(delimiterMarks, i, openedAttributeDelimiter, tags);
                    break;
                default:
                    throw new InternalException($"Unexpected delimiter placement value \"{delimiterMarks[i].Placement}\"");
            }
        }

        return tags;
    }

    private DelimiterMark? ProcessDelimiter(IReadOnlyList<DelimiterMark> delimiterMarks, int i, DelimiterMark? openedDelimiter, List<Tag> tags)
    {
        var delimiter = delimiterMarks[i];

        // close before open
        if (openedDelimiter is null && !delimiter.IsOpen)
            throw new MissingStartDelimiterException(GetPartialTagText(delimiter));

        // open before close
        if (openedDelimiter != null && delimiter.IsOpen)
            throw new MissingCloseDelimiterException(GetPartialTagText(openedDelimiter));

        // valid open
        if (openedDelimiter is null && delimiter.IsOpen)
            openedDelimiter = delimiter;

        // valid close
        if (openedDelimiter != null && !delimiter.IsOpen)
        {
            // create the tag
            var tag = ProcessDelimiterPair(openedDelimiter, delimiter, i, delimiterMarks);
            PopulateTagFields(tag);
            tags.Add(tag);
            openedDelimiter = null;
        }

        return openedDelimiter;
    }

    private static string GetPartialTagText(DelimiterMark delimiter)
    {
        return delimiter switch
        {
            TextNodeDelimiterMark textDelimiter => textDelimiter.XmlTextNode.Text ?? string.Empty,
            AttributeDelimiterMark attrDelimiter => XmlNodes.GetAttributeValue(attrDelimiter.XmlNode, attrDelimiter.AttributeName) ?? string.Empty,
            _ => throw new InternalException($"Unexpected delimiter placement value \"{delimiter.Placement}\"")
        };
    }

    private Tag ProcessDelimiterPair(DelimiterMark openDelimiter, DelimiterMark closeDelimiter, int closeDelimiterIndex, IReadOnlyList<DelimiterMark> allDelimiters)
    {
        if (openDelimiter is TextNodeDelimiterMark openText && closeDelimiter is TextNodeDelimiterMark closeText)
            return ProcessTextNodeDelimiterPair(openText, closeText, closeDelimiterIndex, allDelimiters);

        if (openDelimiter is AttributeDelimiterMark openAttr && closeDelimiter is AttributeDelimiterMark closeAttr)
            return ProcessAttributeDelimiterPair(openAttr, closeAttr);

        throw new InternalException($"Unexpected delimiter placement values. Open delimiter: \"{openDelimiter.Placement}\", Close delimiter: \"{closeDelimiter.Placement}\"");
    }

    private TextNodeTag ProcessTextNodeDelimiterPair(TextNodeDelimiterMark openDelimiter, TextNodeDelimiterMark closeDelimiter, int closeDelimiterIndex, IReadOnlyList<DelimiterMark> allDelimiters)
    {
        // verify tag delimiters are in the same paragraph
        var openTextNode = openDelimiter.XmlTextNode;
        var closeTextNode = closeDelimiter.XmlTextNode;
        var sameNode = openTextNode == closeTextNode;
        if (!sameNode)
        {
            var startParagraph = OfficeMarkup.Query.ContainingParagraphNode(openTextNode);
            var endParagraph = OfficeMarkup.Query.ContainingParagraphNode(closeTextNode);
            if (startParagraph != endParagraph)
                throw new MissingCloseDelimiterException(openTextNode.Text ?? string.Empty);
        }

        // verify no inline drawing in the middle
        var startRun = OfficeMarkup.Query.ContainingRunNode(openTextNode);
        var endRun = OfficeMarkup.Query.ContainingRunNode(closeTextNode);
        OpenXmlElement? currentRun = startRun;
        while (currentRun != null && currentRun != endRun)
        {
            var drawing = currentRun.ChildElements.OfType<Drawing>().FirstOrDefault();
            if (drawing?.ChildElements.OfType<WpInline>().Any() == true)
                throw new MissingCloseDelimiterException(openTextNode.Text ?? string.Empty);

            currentRun = currentRun.NextSibling();
        }

        // normalize the underlying xml structure
        // (make sure the tag's node only includes the tag's text)
        NormalizeTextTagNodes(openDelimiter, closeDelimiter, closeDelimiterIndex, allDelimiters);

        // create the tag
        return new TextNodeTag
        {
            XmlTextNode = openDelimiter.XmlTextNode,
            RawText = openDelimiter.XmlTextNode.Text ?? string.Empty
        };
    }

    private AttributeTag ProcessAttributeDelimiterPair(AttributeDelimiterMark openDelimiter, AttributeDelimiterMark closeDelimiter)
    {
        // verify tag delimiters are in the same attribute
        var openNode = openDelimiter.XmlNode;
        var closeNode = closeDelimiter.XmlNode;
        var attrValue = XmlNodes.GetAttributeValue(openNode, openDelimiter.AttributeName) ?? string.Empty;

        if (openNode != closeNode)
            throw new MissingCloseDelimiterException(attrValue);

        if (openDelimiter.AttributeName != closeDelimiter.AttributeName)
            throw new MissingCloseDelimiterException(attrValue);

        // create the tag
        var tagText = attrValue.Substring(openDelimiter.Index, closeDelimiter.Index + delimiters.TagEnd.Length - openDelimiter.Index);
        return new AttributeTag
        {
            XmlNode = openNode,
            AttributeName = openDelimiter.AttributeName,
            RawText = tagText
        };
    }

    /// <summary>
    /// Consolidate all tag's text into a single text node.
    ///
    /// Example:
    ///
    /// Text node before: "some text {some tag} some more text"
    /// Text nodes after: [ "some text ", "{some tag}", " some more text" ]
    /// </summary>
    private void NormalizeTextTagNodes(
        TextNodeDelimiterMark openDelimiter,
        TextNodeDelimiterMark closeDelimiter,
        int closeDelimiterIndex,
        IReadOnlyList<DelimiterMark> allDelimiters)
    {
        var startTextNode = openDelimiter.XmlTextNode;
        var endTextNode = closeDelimiter.XmlTextNode;
        var sameNode = startTextNode == endTextNode;

        // trim start
        if (openDelimiter.Index > 0)
        {
            OfficeMarkup.Modify.SplitTextNode(startTextNode, openDelimiter.Index, true);
            if (sameNode)
                closeDelimiter.Index -= openDelimiter.Index;
        }

        // trim end
        if (closeDelimiter.Index < (endTextNode.Text?.Length ?? 0) - 1)
        {
            endTextNode = OfficeMarkup.Modify.SplitTextNode(endTextNode, closeDelimiter.Index + delimiters.TagEnd.Length, true);
            if (sameNode)
                startTextNode = endTextNode;
        }

        // join nodes
        if (!sameNode)
        {
            OfficeMarkup.Modify.JoinTextNodesRange(startTextNode, endTextNode);
            endTextNode = startTextNode;
        }

        // update offsets of next delimiters
        for (var i = closeDelimiterIndex + 1; i < allDelimiters.Count; i++)
        {
            var updated = false;
            if (allDelimiters[i] is not TextNodeDelimiterMark curDelimiter)
                break;

            if (curDelimiter.XmlTextNode == openDelimiter.XmlTextNode)
            {
                curDelimiter.Index -= openDelimiter.Index;
                updated = true;
            }

            if (curDelimiter.XmlTextNode == closeDelimiter.XmlTextNode)
            {
                curDelimiter.Index -= closeDelimiter.Index + delimiters.TagEnd.Length;
                updated = true;
            }

            if (!updated)
                break;
        }

        // update references
        openDelimiter.XmlTextNode = startTextNode;
        closeDelimiter.XmlTextNode = endTextNode;
    }

    private void PopulateTagFields(Tag tag)
    {
        if (string.IsNullOrEmpty(tag.RawText))
            throw new InternalException("tag.RawText is required");

        var tagParts = tagRegex.Match(tag.RawText);
        var tagName = tagParts.Groups["tagName"].Value.Trim();

        // ignoring empty tags
        if (tagName.Length == 0)
        {
            tag.Disposition = TagDisposition.SelfClosed;
            return;
        }

        // tag options
        var tagOptionsText = tagParts.Groups["tagOptions"].Value.Trim();
        if (tagOptionsText.Length > 0)
        {
            try
            {
                tag.Options = TagOptionsParser.Parse(TextUtils.NormalizeDoubleQuotes(tagOptionsText));
            }
            catch (FormatException e)
            {
                throw new TagOptionsParseException(tag.RawText, e);
            }
        }

        // container open tag
        if (tagName.StartsWith(delimiters.ContainerTagOpen, StringComparison.Ordinal))
        {
            tag.Disposition = TagDisposition.Open;
            tag.Name = tagName.Substring(delimiters.ContainerTagOpen.Length).Trim();
            return;
        }

        // container close tag
        if (tagName.StartsWith(delimiters.ContainerTagClose, StringComparison.Ordinal))
        {
            tag.Disposition = TagDisposition.Close;
            tag.Name = tagName.Substring(delimiters.ContainerTagClose.Length).Trim();
            return;
        }

        // self-closed tag
        tag.Disposition = TagDisposition.SelfClosed;
        tag.Name = tagName;
    }
}
