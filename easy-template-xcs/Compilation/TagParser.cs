
using System.Text.RegularExpressions;

namespace EasyTemplateXCS.Compilation;

public class TagParser
{
    private readonly Regex tagRegex;
    private readonly Delimiters delimiters;

    public TagParser(Delimiters delimiters)
    {
        if (delimiters == null)
            throw new ArgumentNullException(nameof(delimiters));

        this.delimiters = delimiters;
        this.tagRegex = new Regex($"^{Regex.Escape(delimiters.TagStart)}(.*?){Regex.Escape(delimiters.TagEnd)}", RegexOptions.Multiline);
    }

    public Tag[] Parse(DelimiterMark[] delimiters)
    {
        var tags = new List<Tag>();

        Tag openedTag = null;
        DelimiterMark openedDelimiter = null;
        for (int i = 0; i < delimiters.Length; i++)
        {
            var delimiter = delimiters[i];

            // close before open
            if (openedTag == null && !delimiter.IsOpen)
            {
                var closeTagText = delimiter.XmlTextNode.Text;
                throw new MissingStartDelimiterException(closeTagText);
            }

            // open before close
            if (openedTag != null && delimiter.IsOpen)
            {
                var openTagText = openedDelimiter.XmlTextNode.Text;
                throw new MissingCloseDelimiterException(openTagText);
            }

            // valid open
            if (openedTag == null && delimiter.IsOpen)
            {
                openedTag = new Tag();
                openedDelimiter = delimiter;
            }

            // valid close
            if (openedTag != null && !delimiter.IsOpen)
            {

                // normalize the underlying xml structure
                // (make sure the tag's node only includes the tag's text)
                NormalizeTagNodes(openedDelimiter, delimiter, i, delimiters);
                openedTag.XmlTextNode = openedDelimiter.XmlTextNode;

                // extract tag info from tag's text
                ProcessTag(openedTag);
                tags.Add(openedTag);
                openedTag = null;
                openedDelimiter = null;
            }
        }

        return tags.ToArray();
    }

    /**
     * Consolidate all tag's text into a single text node.
     *
     * Example:
     *
     * Text node before: "some text {some tag} some more text"
     * Text nodes after: [ "some text ", "{some tag}", " some more text" ]
     */
    private void NormalizeTagNodes(
        DelimiterMark openDelimiter,
        DelimiterMark closeDelimiter,
        int closeDelimiterIndex,
        DelimiterMark[] allDelimiters
    )
    {

        var startTextNode = openDelimiter.XmlTextNode;
        var endTextNode = closeDelimiter.XmlTextNode;
        var sameNode = (startTextNode == endTextNode);

        // trim start
        if (openDelimiter.Index > 0)
        {
            DocParserHelpers.SplitTextNode(startTextNode, openDelimiter.Index, true);
            if (sameNode)
            {
                closeDelimiter.Index -= openDelimiter.Index;
            }
        }

        // trim end
        if (closeDelimiter.Index < endTextNode.Text.Length - 1)
        {
            DocParserHelpers.SplitTextNode(endTextNode, closeDelimiter.Index + this.delimiters.TagEnd.Length, true);
            if (sameNode)
            {
                startTextNode = endTextNode;
            }
        }

        // join nodes
        if (!sameNode)
        {
            DocParserHelpers.JoinTextNodesRange(startTextNode, endTextNode);
            endTextNode = startTextNode;
        }

        // update offsets of next delimiters
        for (int i = closeDelimiterIndex + 1; i < allDelimiters.Length; i++)
        {

            bool updated = false;
            var curDelimiter = allDelimiters[i];

            if (curDelimiter.XmlTextNode == openDelimiter.XmlTextNode)
            {
                curDelimiter.Index -= openDelimiter.Index;
                updated = true;
            }

            if (curDelimiter.XmlTextNode == closeDelimiter.XmlTextNode)
            {
                curDelimiter.Index -= closeDelimiter.Index + this.delimiters.TagEnd.Length;
                updated = true;
            }

            if (!updated)
                break;
        }

        // update references
        openDelimiter.XmlTextNode = startTextNode;
        closeDelimiter.XmlTextNode = endTextNode;
    }

    private void ProcessTag(Tag tag)
    {
        tag.RawText = tag.XmlTextNode.Text;

        var tagParts = this.tagRegex.Match(tag.RawText);
        var tagContent = (tagParts.Groups[1].Value ?? "").Trim();
        if (string.IsNullOrEmpty(tagContent))
        {
            tag.Disposition = TagDisposition.SelfClosed;
            return;
        }

        if (tagContent.StartsWith(this.delimiters.ContainerTagOpen))
        {
            tag.Disposition = TagDisposition.Open;
            tag.Name = tagContent.Substring(this.delimiters.ContainerTagOpen.Length).Trim();

        }
        else if (tagContent.StartsWith(this.delimiters.ContainerTagClose))
        {
            tag.Disposition = TagDisposition.Close;
            tag.Name = tagContent.Substring(this.delimiters.ContainerTagClose.Length).Trim();

        }
        else
        {
            tag.Disposition = TagDisposition.SelfClosed;
            tag.Name = tagContent;
        }
    }
}