namespace Easy.Template.XCS.Plugins;

public abstract class PluginContent { 
    public string _type { get; set; }

    public static bool IsPluginContent(dynamic content)
    {
        return false; 
    }
}