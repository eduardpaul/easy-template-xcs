using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;
using Easy.Template.XCS.Xml;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using WpInline = DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline;

namespace Easy.Template.XCS.Office;

//
// Wordprocessing Markup Language (WML) intro:
//
// In Word text nodes are contained in "run" nodes (which specifies text
// properties such as font and color). The "run" nodes in turn are
// contained in paragraph nodes which is the core unit of content.
//
// Example:
//
// <w:p>    <-- paragraph
//   <w:r>      <-- run
//     <w:rPr>      <-- run properties
//       <w:b/>     <-- bold
//     </w:rPr>
//     <w:t>This is text.</w:t>     <-- actual text
//   </w:r>
// </w:p>
//
// - For an easy introduction, see: http://officeopenxml.com/WPcontentOverview.php
// - For the complete specification, see: https://ecma-international.org/publications-and-standards/standards/ecma-376/
//

/// <summary>
/// Wordprocessing Markup Language (WML) utilities.
/// </summary>
public static class OfficeMarkup
{
    /// <summary>
    /// Wordprocessing markup query utilities.
    /// </summary>
    public static class Query
    {
        public static bool IsTextNode(OpenXmlElement? node) => node is Text;

        public static bool IsRunNode(OpenXmlElement? node) => node is Run;

        public static bool IsRunPropertiesNode(OpenXmlElement? node) => node is RunProperties;

        public static bool IsTableNode(OpenXmlElement? node) => node is Table;

        public static bool IsTableRowNode(OpenXmlElement? node) => node is TableRow;

        public static bool IsTableCellNode(OpenXmlElement? node) => node is TableCell;

        public static bool IsParagraphNode(OpenXmlElement? node) => node is Paragraph;

        public static bool IsParagraphPropertiesNode(OpenXmlElement? node) => node is ParagraphProperties;

        public static bool IsListParagraph(OpenXmlElement paragraphNode)
        {
            var paragraphProperties = FindParagraphPropertiesNode(paragraphNode);
            return paragraphProperties?.NumberingProperties != null;
        }

        public static bool IsInlineDrawingNode(OpenXmlElement? node)
        {
            return node is WpInline && node.Parent is Drawing;
        }

        public static ParagraphProperties? FindParagraphPropertiesNode(OpenXmlElement paragraphNode)
        {
            if (paragraphNode is not Paragraph paragraph)
                throw new ArgumentException($"Expected paragraph node but received a '{paragraphNode.LocalName}' node.");

            return paragraph.ParagraphProperties;
        }

        /// <summary>
        /// Search for the first direct child text node (i.e. a <c>w:t</c> node) of a run.
        /// </summary>
        public static Text? FirstTextNodeChild(OpenXmlElement? node)
        {
            if (node is not Run run)
                return null;

            return run.ChildElements.OfType<Text>().FirstOrDefault();
        }

        /// <summary>
        /// Search upwards for the first run node.
        /// </summary>
        public static Run? ContainingRunNode(OpenXmlElement? node) => XmlNodes.FindParent<Run>(node);

        /// <summary>
        /// Search upwards for the first paragraph node.
        /// </summary>
        public static Paragraph? ContainingParagraphNode(OpenXmlElement? node) => XmlNodes.FindParent<Paragraph>(node);

        /// <summary>
        /// Search upwards for the first table row node.
        /// </summary>
        public static TableRow? ContainingTableRowNode(OpenXmlElement? node) => XmlNodes.FindParent<TableRow>(node);

        /// <summary>
        /// Search upwards for the first table cell node.
        /// </summary>
        public static TableCell? ContainingTableCellNode(OpenXmlElement? node) => XmlNodes.FindParent<TableCell>(node);

        /// <summary>
        /// Search upwards for the first table node.
        /// </summary>
        public static Table? ContainingTableNode(OpenXmlElement? node) => XmlNodes.FindParent<Table>(node);

        /// <summary>
        /// Search upwards for the first <c>w:sdtContent</c> node.
        /// </summary>
        public static OpenXmlElement? ContainingStructuredTagContentNode(OpenXmlElement? node)
        {
            return XmlNodes.FindParent(node, n => n is SdtContentBlock or SdtContentRun or SdtContentCell or SdtContentRow or SdtContentRunRuby);
        }

        public static bool IsEmptyTextNode(OpenXmlElement node)
        {
            if (node is not Text text)
                throw new ArgumentException($"Text node expected but '{node.LocalName}' received.");

            return string.IsNullOrEmpty(text.Text);
        }

        public static bool IsEmptyRun(OpenXmlElement node)
        {
            if (node is not Run)
                throw new ArgumentException($"Run node expected but '{node.LocalName}' received.");

            foreach (var child in node.ChildElements)
            {
                if (child is RunProperties)
                    continue;

                if (child is Text && IsEmptyTextNode(child))
                    continue;

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Wordprocessing markup modify utilities.
    /// </summary>
    public static class Modify
    {
        /// <summary>
        /// Split the text node into two text nodes, each with it's own <c>w:t</c> node.
        /// Returns the newly created text node.
        /// </summary>
        /// <param name="textNode">The text node to split.</param>
        /// <param name="splitIndex">The index to split at.</param>
        /// <param name="addBefore">Should the new node be added before or after the original node.</param>
        public static Text SplitTextNode(Text textNode, int splitIndex, bool addBefore)
        {
            Text firstTextNode;
            Text secondTextNode;

            // split nodes
            var newTextNode = (Text)textNode.CloneNode(true);

            // set space preserve to prevent display differences after splitting
            // (otherwise if there was a space in the middle of the text node and it
            // is now at the beginning or end of the text node it will be ignored)
            SetSpacePreserveAttribute(textNode);
            SetSpacePreserveAttribute(newTextNode);

            if (addBefore)
            {
                // insert new node before existing one
                textNode.InsertBeforeSelf(newTextNode);
                firstTextNode = newTextNode;
                secondTextNode = textNode;
            }
            else
            {
                // insert new node after existing one
                textNode.InsertAfterSelf(newTextNode);
                firstTextNode = textNode;
                secondTextNode = newTextNode;
            }

            // edit text
            var firstText = firstTextNode.Text ?? string.Empty;
            var secondText = secondTextNode.Text ?? string.Empty;
            firstTextNode.Text = firstText.Substring(0, splitIndex);
            secondTextNode.Text = secondText.Substring(splitIndex);

            return addBefore ? firstTextNode : secondTextNode;
        }

        /// <summary>
        /// Split the paragraph around the specified text node.
        /// </summary>
        /// <returns>
        /// Two paragraphs - 'left' and 'right'. If the 'removeTextNode' argument is
        /// false then the original text node is the first text node of 'right'.
        /// </returns>
        public static (Paragraph Left, Paragraph Right) SplitParagraphByTextNode(OpenXmlElement paragraph, Text textNode, bool removeTextNode)
        {
            // input validation
            var containingParagraph = Query.ContainingParagraphNode(textNode);
            if (containingParagraph != paragraph)
                throw new ArgumentException("Node 'textNode' is not contained in the specified paragraph.");

            var runNode = Query.ContainingRunNode(textNode)
                ?? throw new ArgumentException("Node 'textNode' is not contained in a run.");

            //
            // 1. Split the run
            //

            // create run clone (left) and keep the original run (right)
            var leftRun = (Run)runNode.CloneNode(false);
            var rightRun = runNode;
            rightRun.InsertBeforeSelf(leftRun);

            // copy props from original run node (preserve style)
            var runProps = rightRun.ChildElements.OfType<RunProperties>().FirstOrDefault();
            if (runProps != null)
                leftRun.AppendChild(runProps.CloneNode(true));

            // move all nodes up to the specified text node, to the new run
            var firstRunChildIndex = runProps != null ? 1 : 0;
            var curChild = rightRun.ChildElements[firstRunChildIndex];
            while (curChild != textNode)
            {
                curChild.Remove();
                leftRun.AppendChild(curChild);
                curChild = rightRun.ChildElements[firstRunChildIndex];
            }

            // remove text node
            if (removeTextNode)
                textNode.Remove();

            //
            // 2. Split the paragraph
            //

            // create paragraph clone (left) and keep the original paragraph (right)
            var leftPara = (Paragraph)containingParagraph.CloneNode(false);
            var rightPara = containingParagraph;
            rightPara.InsertBeforeSelf(leftPara);

            // copy props from original paragraph (preserve style)
            var paragraphProps = rightPara.ChildElements.OfType<ParagraphProperties>().FirstOrDefault();
            if (paragraphProps != null)
                leftPara.AppendChild(paragraphProps.CloneNode(true));

            // move all run nodes up to the original run (right), to the new paragraph (left)
            var firstParaChildIndex = paragraphProps != null ? 1 : 0;
            curChild = rightPara.ChildElements[firstParaChildIndex];
            while (curChild != rightRun)
            {
                curChild.Remove();
                leftPara.AppendChild(curChild);
                curChild = rightPara.ChildElements[firstParaChildIndex];
            }

            // clean paragraphs - remove empty runs
            if (Query.IsEmptyRun(leftRun))
                leftRun.Remove();
            if (Query.IsEmptyRun(rightRun))
                rightRun.Remove();

            return (leftPara, rightPara);
        }

        /// <summary>
        /// Move all text between the 'from' and 'to' nodes to the 'from' node.
        /// </summary>
        public static void JoinTextNodesRange(Text from, Text to)
        {
            // find run nodes
            var firstRunNode = Query.ContainingRunNode(from) ?? throw new ArgumentException("Node 'from' is not contained in a run.");
            var secondRunNode = Query.ContainingRunNode(to) ?? throw new ArgumentException("Node 'to' is not contained in a run.");

            var paragraphNode = firstRunNode.Parent;
            if (secondRunNode.Parent != paragraphNode)
                throw new ArgumentException("Can not join text nodes from separate paragraphs.");

            var totalText = new List<string>();

            // iterate runs
            OpenXmlElement? curRunNode = firstRunNode;
            while (curRunNode != null)
            {
                // iterate text nodes
                OpenXmlElement? curTextNode = curRunNode == firstRunNode ? from : Query.FirstTextNodeChild(curRunNode);
                while (curTextNode != null)
                {
                    if (curTextNode is not Text text)
                    {
                        curTextNode = curTextNode.NextSibling();
                        continue;
                    }

                    // move text to first node
                    totalText.Add(text.Text ?? string.Empty);

                    // next text node
                    var textToRemove = curTextNode;
                    curTextNode = curTextNode == to ? null : curTextNode.NextSibling();

                    // remove current text node
                    if (textToRemove != from)
                        textToRemove.Remove();
                }

                // next run
                var runToRemove = curRunNode;
                curRunNode = curRunNode == secondRunNode ? null : curRunNode.NextSibling();

                // remove current run
                if (!runToRemove.HasChildren)
                    runToRemove.Remove();
            }

            // set the text content
            from.Text = string.Concat(totalText);
        }

        /// <summary>
        /// Take all runs from 'second' and move them to 'first'.
        /// </summary>
        public static void JoinParagraphs(OpenXmlElement first, OpenXmlElement second)
        {
            if (first == second)
                return;

            var curChild = second.FirstChild;
            while (curChild != null)
            {
                var nextChild = curChild.NextSibling();
                if (curChild is Run)
                {
                    curChild.Remove();
                    first.AppendChild(curChild);
                }
                curChild = nextChild;
            }
        }

        public static void SetSpacePreserveAttribute(Text node)
        {
            node.Space ??= SpaceProcessingModeValues.Preserve;
        }

        /// <summary>
        /// Remove the tag from the document.
        /// </summary>
        public static void RemoveTag(Tag tag)
        {
            switch (tag)
            {
                case TextNodeTag textNodeTag:
                    {
                        var textNode = textNodeTag.XmlTextNode;
                        var runNode = Query.ContainingRunNode(textNode);

                        // remove the text node
                        XmlNodes.Remove(textNode);

                        // remove the run node if it's empty
                        if (runNode != null && Query.IsEmptyRun(runNode))
                            XmlNodes.Remove(runNode);
                        return;
                    }

                case AttributeTag attributeTag:
                    {
                        var attrValue = XmlNodes.GetAttributeValue(attributeTag.XmlNode, attributeTag.AttributeName);
                        if (attrValue is null)
                            return;

                        // remove the tag from the attribute value
                        var newValue = ReplaceFirst(attrValue, attributeTag.RawText, string.Empty);

                        // remove the attribute if it's empty
                        XmlNodes.SetAttributeValue(attributeTag.XmlNode, attributeTag.AttributeName, newValue.Length == 0 ? null : newValue);
                        return;
                    }

                default:
                    throw new ArgumentException($"Unexpected tag placement \"{tag.Placement}\" for tag \"{tag.RawText}\".");
            }
        }

        public static string ReplaceFirst(string text, string search, string replacement)
        {
            var index = text.IndexOf(search, StringComparison.Ordinal);
            if (index < 0)
                return text;
            return text.Substring(0, index) + replacement + text.Substring(index + search.Length);
        }
    }
}
