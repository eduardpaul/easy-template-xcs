using EasyTemplateXCS.Compilation;

namespace EasyTemplateXCS.Plugins;

public class PluginUtilities
{
    public TemplateCompiler Compiler { get; set; }
}

public abstract class TemplatePlugin
{
    public abstract string ContentType { get; }

    protected PluginUtilities Utilities;

    public virtual void SetUtilities(PluginUtilities utilities)
    {
        Utilities = utilities;
    }

    public virtual Task SimpleTagReplacements(Tag tag, ScopeData data, TemplateContext context)
    {
        return Task.CompletedTask;
    }

    public virtual Task ContainerTagReplacements(List<Tag> tags, ScopeData data, TemplateContext context)
    {
        return Task.CompletedTask;
    }
}