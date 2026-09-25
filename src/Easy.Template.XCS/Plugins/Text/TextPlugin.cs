using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Utils;
using Easy.Template.XCS.Xml;
using WText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Easy.Template.XCS.Plugins.Text;

public class TextPlugin : TemplatePlugin
{
    public const string ContentTypeName = "text";

    public override string ContentType => ContentTypeName;

    /// <summary>
    /// Replace the node text content with the specified value.
    /// </summary>
    public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        var value = data.GetScopeData();
        var strValue = TemplateData.StringValue(value);

        switch (tag)
        {
            case TextNodeTag textNodeTag:
                ReplaceInTextNode(textNodeTag, strValue);
                break;
            case AttributeTag attributeTag:
                ReplaceInAttribute(attributeTag, strValue);
                break;
            default:
                throw new TemplateSyntaxException($"Unexpected tag placement \"{tag.Placement}\" for tag \"{tag.RawText}\".");
        }

        return Task.CompletedTask;
    }

    private static void ReplaceInTextNode(TextNodeTag tag, string text)
    {
        var lines = text.Split('\n');
        if (lines.Length < 2)
            ReplaceSingleLine(tag, lines.Length > 0 ? lines[0] : string.Empty);
        else
            ReplaceMultiLine(tag, lines);
    }

    private static void ReplaceInAttribute(AttributeTag tag, string text)
    {
        var attrValue = XmlNodes.GetAttributeValue(tag.XmlNode, tag.AttributeName) ?? string.Empty;

        // set text
        var newValue = OfficeMarkup.Modify.ReplaceFirst(attrValue, tag.RawText, text);
        XmlNodes.SetAttributeValue(tag.XmlNode, tag.AttributeName, newValue);

        // remove the attribute if it's empty
        if (text.Length == 0)
            OfficeMarkup.Modify.RemoveTag(tag);
    }

    private static void ReplaceSingleLine(TextNodeTag tag, string text)
    {
        // set text
        var textNode = tag.XmlTextNode;
        textNode.Text = text;

        // clean up if the text node is now empty
        if (text.Length == 0)
        {
            OfficeMarkup.Modify.RemoveTag(tag);
            return;
        }

        // make sure leading and trailing whitespace are preserved
        OfficeMarkup.Modify.SetSpacePreserveAttribute(textNode);
    }

    private static void ReplaceMultiLine(TextNodeTag tag, string[] lines)
    {
        var textNode = tag.XmlTextNode;

        // first line
        var firstLine = lines[0];
        textNode.Text = firstLine;
        OfficeMarkup.Modify.SetSpacePreserveAttribute(textNode);

        // other lines
        OpenXmlElement insertAfter = textNode;
        for (var i = 1; i < lines.Length; i++)
        {
            // add line break
            var lineBreak = new Break();
            insertAfter.InsertAfterSelf(lineBreak);
            insertAfter = lineBreak;

            // add text
            if (lines[i].Length > 0)
            {
                var lineNode = CreateTextNode(lines[i]);
                insertAfter.InsertAfterSelf(lineNode);
                insertAfter = lineNode;
            }
        }

        // clean up if the original text node is now empty
        if (firstLine.Length == 0)
            XmlNodes.Remove(textNode);
    }

    private static WText CreateTextNode(string text)
    {
        var textNode = new WText(text);
        OfficeMarkup.Modify.SetSpacePreserveAttribute(textNode);
        return textNode;
    }
}
