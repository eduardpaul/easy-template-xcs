using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Xml;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;

namespace Easy.Template.XCS.Compilation.Delimiters;

/// <summary>
/// Search for tags in xml attributes.
/// Currently only the description (alt text) attribute of drawing objects is supported.
/// </summary>
public class AttributesDelimiterSearcher
{
    public const string DrawingDescriptionAttributeName = "descr";

    private readonly HashSet<OpenXmlElement> visitedNodes = new(ReferenceEqualityComparer.Instance);
    private readonly XCS.Delimiters delimiters;
    private readonly Regex tagRegex;

    public AttributesDelimiterSearcher(XCS.Delimiters delimiters, Regex? tagRegex = null)
    {
        if (delimiters is null)
            throw new InternalArgumentMissingException(nameof(delimiters));

        this.delimiters = delimiters;
        this.tagRegex = tagRegex ?? TagUtils.TagRegex(delimiters);
    }

    public void ProcessNode(XmlTreeIterator it, List<DelimiterMark> delimiterMarks)
    {
        // ignore irrelevant nodes
        if (!ShouldSearchNode(it.Node))
            return;

        // search delimiters in attributes
        // (currently we only support description attributes of drawing objects)
        FindDelimitersInAttribute(it.Node!, DrawingDescriptionAttributeName, delimiterMarks);
    }

    private bool ShouldSearchNode(OpenXmlElement? node)
    {
        if (node is null)
            return false;

        if (!visitedNodes.Add(node))
            return false;

        if (!IsDrawingPropertiesNode(node))
            return false;

        return !string.IsNullOrEmpty(XmlNodes.GetAttributeValue(node, DrawingDescriptionAttributeName));
    }

    private static bool IsDrawingPropertiesNode(OpenXmlElement node)
    {
        // node is drawing properties
        if (node is not DocProperties)
            return false;

        // parent is drawing
        if (node.Parent is null)
            return false;

        return XmlNodes.FindParent<Drawing>(node) != null;
    }

    private void FindDelimitersInAttribute(OpenXmlElement node, string attributeName, List<DelimiterMark> delimiterMarks)
    {
        var attrValue = XmlNodes.GetAttributeValue(node, attributeName);
        if (string.IsNullOrEmpty(attrValue))
            return;

        foreach (Match match in tagRegex.Matches(attrValue))
        {
            var tag = match.Value;
            var openDelimiterIndex = match.Index;
            var closeDelimiterIndex = openDelimiterIndex + tag.Length - delimiters.TagEnd.Length;

            delimiterMarks.Add(CreateDelimiterMark(openDelimiterIndex, true, node, attributeName));
            delimiterMarks.Add(CreateDelimiterMark(closeDelimiterIndex, false, node, attributeName));
        }
    }

    private static AttributeDelimiterMark CreateDelimiterMark(int index, bool isOpen, OpenXmlElement xmlNode, string attributeName)
    {
        return new AttributeDelimiterMark
        {
            IsOpen = isOpen,
            Index = index,
            AttributeName = attributeName,
            XmlNode = xmlNode
        };
    }
}
