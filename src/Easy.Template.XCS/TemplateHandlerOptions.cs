using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Extensions;
using Easy.Template.XCS.Plugins;
using Easy.Template.XCS.Plugins.Loop;
using Easy.Template.XCS.Plugins.Text;

namespace Easy.Template.XCS;

public class TemplateHandlerOptions
{
    public IList<TemplatePlugin> Plugins { get; set; } = DefaultPlugins.Create();

    /// <summary>
    /// Determines the behavior in case of an empty input data. If set to true
    /// the tag will be left untouched, if set to false the tag will be replaced
    /// by an empty string.
    ///
    /// Default: false
    /// </summary>
    public bool SkipEmptyTags { get; set; }

    public string DefaultContentType { get; set; } = TextPlugin.ContentTypeName;

    public string ContainerContentType { get; set; } = LoopPlugin.ContentTypeName;

    public Delimiters Delimiters { get; set; } = new();

    public int MaxXmlDepth { get; set; } = 20;

    public ExtensionOptions Extensions { get; set; } = new();

    /// <summary>
    /// Custom scope data resolver. If not set, the default resolver is used
    /// (see <see cref="ScopeData.DefaultResolver"/>).
    /// </summary>
    public ScopeDataResolver? ScopeDataResolver { get; set; }

    public TemplateHandlerOptions()
    {
    }

    /// <summary>
    /// Create a validated copy of the specified options.
    /// </summary>
    public TemplateHandlerOptions(TemplateHandlerOptions? initial)
    {
        if (initial != null)
        {
            Plugins = new List<TemplatePlugin>(initial.Plugins);
            SkipEmptyTags = initial.SkipEmptyTags;
            DefaultContentType = initial.DefaultContentType;
            ContainerContentType = initial.ContainerContentType;
            Delimiters = new Delimiters(initial.Delimiters);
            MaxXmlDepth = initial.MaxXmlDepth;
            Extensions = initial.Extensions;
            ScopeDataResolver = initial.ScopeDataResolver;
        }
        else
        {
            Delimiters = new Delimiters(Delimiters);
        }

        if (Plugins.Count == 0)
            throw new ArgumentException("Plugins list can not be empty");

        if (MaxXmlDepth <= 0)
            throw new ArgumentException("MaxXmlDepth must be a positive number");
    }
}
