using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Easy.Template.XCS.Compilation;

public enum TagDisposition
{
    Open,
    Close,
    SelfClosed
}

public enum TagPlacement
{
    /// <summary>
    /// The tag is placed inside a text node (a <c>w:t</c> element).
    /// </summary>
    TextNode,

    /// <summary>
    /// The tag is placed inside an xml attribute (for instance, the alt text of an image).
    /// </summary>
    Attribute
}

public abstract class Tag
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Options specified in the tag itself, e.g. <c>{#loop [loopOver: "row"]}</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Options { get; set; }

    /// <summary>
    /// The full tag text, for instance: "{#my-tag}".
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    public TagDisposition Disposition { get; set; }

    public abstract TagPlacement Placement { get; }

    public override string ToString() => RawText;
}

public sealed class TextNodeTag : Tag
{
    public override TagPlacement Placement => TagPlacement.TextNode;

    /// <summary>
    /// The <c>w:t</c> node that contains (only) the tag text.
    /// </summary>
    public required Text XmlTextNode { get; set; }
}

public sealed class AttributeTag : Tag
{
    public override TagPlacement Placement => TagPlacement.Attribute;

    public required OpenXmlElement XmlNode { get; set; }

    public required string AttributeName { get; set; }
}
