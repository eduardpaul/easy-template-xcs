using EasyTemplateXCS.Plugins;
using EasyTemplateXCS.Compilation;
using EasyTemplateXCS.Plugins.TextPlugin;

namespace EasyTemplateXCS;

public class TemplateHandlerOptions
{
    const string TEXT_CONTENT_TYPE = "text";
    const string LOOP_CONTENT_TYPE = "loop";

    public List<TemplatePlugin> Plugins { get; set; } = CreateDefaultPlugins();
    public bool SkipEmptyTags { get; set; } = false;
    public string DefaultContentType { get; set; } = TEXT_CONTENT_TYPE;
    public string ContainerContentType { get; set; } = LOOP_CONTENT_TYPE;
    public Delimiters Delimiters { get; set; } = new Delimiters();
    public int MaxXmlDepth { get; set; } = 20;
    //public ExtensionOptions Extensions { get; set; } = new ExtensionOptions();
    public ScopeDataResolver ScopeDataResolver { get; set; }

    public TemplateHandlerOptions(TemplateHandlerOptions initial = null)
    {
        if (initial != null)
        {
            Plugins = CreateDefaultPlugins();
            SkipEmptyTags = initial.SkipEmptyTags;
            DefaultContentType = initial.DefaultContentType ?? TEXT_CONTENT_TYPE;
            ContainerContentType = initial.ContainerContentType ?? LOOP_CONTENT_TYPE;
            Delimiters = new Delimiters(initial.Delimiters);
            MaxXmlDepth = initial.MaxXmlDepth;
            //Extensions = initial.Extensions ?? new ExtensionOptions();
            ScopeDataResolver = initial.ScopeDataResolver;
        }

        if (Plugins.Count == 0)
        {
            throw new Exception("Plugins list can not be empty");
        }
    }

    private static List<TemplatePlugin> CreateDefaultPlugins()
    {
        return new List<TemplatePlugin>
        {
            // new LoopPlugin(),
            // new RawXmlPlugin(),
            // new ImagePlugin(),
            // new LinkPlugin(),
            new TextPlugin()
        };
    }
}