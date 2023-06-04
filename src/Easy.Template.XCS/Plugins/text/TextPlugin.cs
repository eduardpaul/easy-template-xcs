using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;

namespace Easy.Template.XCS.Plugins.TextPlugin;

public class TextPlugin : TemplatePlugin
{
    public override string ContentType => "text";

    public override Task SimpleTagReplacements(Tag tag, ScopeData data, TemplateContext context)
    {
        var value = data.GetScopeData();
        var lines = value.ToString().Split('\n');

        if (lines.Length < 2)
        {
            ReplaceSingleLine(tag.XmlTextNode, lines.Length > 0 ? lines[0] : "");
        }
        else
        {
            ReplaceMultiLine(tag.XmlTextNode, lines);
        }

        return Task.CompletedTask;
    }

    private void ReplaceSingleLine(Text textNode, string text)
    {
        // set text
        textNode.Text = text;

        // make sure leading and trailing whitespace are preserved
        textNode.SetAttribute(new OpenXmlAttribute("space", "xml", "preserve"));
    }

    private void ReplaceMultiLine(Text textNode, string[] lines)
    {
        // first line
        textNode.Text = lines[0];

        // other lines
        for (var i = 1; i < lines.Length; i++)
        {
            // add line break
            textNode.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Break()));

            // add text
            var lineNode = CreateWordTextNode(lines[i]);
            textNode.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(lineNode));
        }
    }

    private Text CreateWordTextNode(string text)
    {
        var textNode = new Text(text);

        textNode.SetAttribute(new OpenXmlAttribute("space", "xml", "preserve"));

        return textNode;
    }
}
