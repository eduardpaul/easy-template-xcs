namespace Easy.Template.XCS.Plugins.RawXml;

public class RawXmlContent : PluginContent
{
    public const string ContentTypeName = "rawXml";

    public override string ContentType => ContentTypeName;

    /// <summary>
    /// The raw xml markup to insert. Can be a single string or a list of strings.
    /// </summary>
    public object? Xml { get; set; }

    /// <summary>
    /// Replace a part of the document with raw xml content.
    /// If set to true the plugin will replace the parent paragraph (<c>w:p</c>) of
    /// the tag, otherwise it will replace the parent text node (<c>w:t</c>).
    /// </summary>
    public bool ReplaceParagraph { get; set; }

    public RawXmlContent()
    {
    }

    public RawXmlContent(string xml, bool replaceParagraph = false)
    {
        Xml = xml;
        ReplaceParagraph = replaceParagraph;
    }

    public RawXmlContent(IEnumerable<string> xml, bool replaceParagraph = false)
    {
        Xml = xml.ToList();
        ReplaceParagraph = replaceParagraph;
    }

    /// <summary>
    /// The xml content as a single string, or null if not specified.
    /// </summary>
    public string? GetXmlString()
    {
        return Xml switch
        {
            null => null,
            string s => s,
            IEnumerable<string> list => string.Concat(list),
            _ => null
        };
    }
}
