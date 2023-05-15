namespace EasyTemplateXCS.Plugins;

public interface PluginContent
{
    string _type { get; set; }
}

public static class PluginContentExtensions
{
    public static bool IsPluginContent(this object content)
    {
        return content is PluginContent;
    }
}