using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;

namespace Easy.Template.XCS.Plugins.RawXmlPlugin;

public class RawXmlPlugin  : TemplatePlugin
{
    public override string ContentType => "rawXml";

    public override Task SimpleTagReplacements(Tag tag, ScopeData data, TemplateContext context)
    {
        var value = data.GetScopeData() as RawXmlContent;

        /*
        const value = data.getScopeData<RawXmlContent>();

        const replaceNode = value?.replaceParagraph ?
            this.utilities.docxParser.containingParagraphNode(tag.xmlTextNode) :
            this.utilities.docxParser.containingTextNode(tag.xmlTextNode);

        if (typeof value?.xml === 'string') {
            const newNode = this.utilities.xmlParser.parse(value.xml);
            XmlNode.insertBefore(newNode, replaceNode);
        }

        XmlNode.remove(replaceNode);
        */





        return Task.CompletedTask;
    }
}