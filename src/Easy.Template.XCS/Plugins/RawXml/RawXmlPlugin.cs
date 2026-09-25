using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Utils;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Plugins.RawXml;

public class RawXmlPlugin : TemplatePlugin
{
    public override string ContentType => RawXmlContent.ContentTypeName;

    /// <summary>
    /// Namespace declarations made available to raw xml snippets, so that
    /// common prefixes (w, r, wp, a, pic, ...) can be used without declaring them.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> KnownNamespaces = new Dictionary<string, string>
    {
        ["w"] = "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
        ["r"] = "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
        ["wp"] = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
        ["a"] = "http://schemas.openxmlformats.org/drawingml/2006/main",
        ["pic"] = "http://schemas.openxmlformats.org/drawingml/2006/picture",
        ["c"] = "http://schemas.openxmlformats.org/drawingml/2006/chart",
        ["m"] = "http://schemas.openxmlformats.org/officeDocument/2006/math",
        ["v"] = "urn:schemas-microsoft-com:vml",
        ["o"] = "urn:schemas-microsoft-com:office:office",
        ["w10"] = "urn:schemas-microsoft-com:office:word",
        ["mc"] = "http://schemas.openxmlformats.org/markup-compatibility/2006",
        ["w14"] = "http://schemas.microsoft.com/office/word/2010/wordml",
        ["w15"] = "http://schemas.microsoft.com/office/word/2012/wordml",
        ["wps"] = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape",
        ["wpg"] = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup",
        ["wp14"] = "http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing",
        ["a14"] = "http://schemas.microsoft.com/office/drawing/2010/main",
        ["xml"] = "http://www.w3.org/XML/1998/namespace",
    };

    public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        if (tag is not TextNodeTag textNodeTag)
            throw new TemplateSyntaxException($"RawXml tag \"{tag.RawText}\" must be placed in a text node but was placed in {tag.Placement}");

        var value = data.GetScopeData<RawXmlContent>();

        OpenXmlElement replaceNode = value?.ReplaceParagraph == true
            ? OfficeMarkup.Query.ContainingParagraphNode(textNodeTag.XmlTextNode) ?? throw new TemplateSyntaxException($"Tag {tag.RawText} is not inside a paragraph.")
            : textNodeTag.XmlTextNode;

        var xmlContent = value?.GetXmlString();
        if (xmlContent != null)
        {
            // parse the xml content
            var children = ParseFragment(xmlContent);

            // insert the xml content
            foreach (var child in children)
                XmlNodes.InsertBefore(child, replaceNode);
        }

        if (value?.ReplaceParagraph == true)
            XmlNodes.Remove(replaceNode);
        else
            OfficeMarkup.Modify.RemoveTag(textNodeTag);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Parse an xml fragment (zero or more elements) into OpenXml elements.
    /// </summary>
    public static List<OpenXmlElement> ParseFragment(string xmlContent)
    {
        var declarations = string.Join(" ", KnownNamespaces.Where(kv => kv.Key != "xml").Select(kv => $"xmlns:{kv.Key}=\"{kv.Value}\""));
        var wrapped = $"<w:body {declarations}>{xmlContent}</w:body>";

        List<OpenXmlElement> children;
        try
        {
            var body = new Body(wrapped);
            children = body.ChildElements.ToList();
            foreach (var child in children)
                child.Remove();
        }
        catch (Exception e) when (e is XmlException or InvalidOperationException or ArgumentException)
        {
            throw new TemplateDataException($"Failed to parse raw xml content: {e.Message}");
        }

        return children;
    }
}
