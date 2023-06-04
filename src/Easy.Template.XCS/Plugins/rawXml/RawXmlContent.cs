namespace Easy.Template.XCS.Plugins.RawXmlPlugin;

public class RawXmlContent : PluginContent
{
    public string xml { get; set; }

    /**
     * Replace a part of the document with raw xml content.
     * If set to `true` the plugin will replace the parent paragraph (<w:p>) of
     * the tag, otherwise it will replace the parent text node (<w:t>).
     */
    public bool? replaceParagraph { get; set; }
}