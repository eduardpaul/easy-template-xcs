using Easy.Template.XCS.Plugins.Image;
using Easy.Template.XCS.Plugins.Link;
using Easy.Template.XCS.Plugins.Loop;
using Easy.Template.XCS.Plugins.RawXml;
using Easy.Template.XCS.Plugins.Text;

namespace Easy.Template.XCS.Plugins;

public static class DefaultPlugins
{
    public static List<TemplatePlugin> Create()
    {
        return new List<TemplatePlugin>
        {
            new LoopPlugin(),
            new RawXmlPlugin(),
            new ImagePlugin(),
            new LinkPlugin(),
            new TextPlugin()
        };
    }
}
