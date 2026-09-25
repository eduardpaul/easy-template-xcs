using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Easy.Template.XCS.Compilation.Delimiters;

public abstract class DelimiterMark
{
    /// <summary>
    /// Is this an open delimiter or a close delimiter.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Index inside the text node / attribute value.
    /// </summary>
    public int Index { get; set; }

    public abstract TagPlacement Placement { get; }
}

public sealed class TextNodeDelimiterMark : DelimiterMark
{
    public override TagPlacement Placement => TagPlacement.TextNode;

    public required Text XmlTextNode { get; set; }
}

public sealed class AttributeDelimiterMark : DelimiterMark
{
    public override TagPlacement Placement => TagPlacement.Attribute;

    public required OpenXmlElement XmlNode { get; set; }

    public required string AttributeName { get; set; }
}
