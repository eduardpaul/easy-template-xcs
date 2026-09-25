using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Plugins;

public sealed class PluginUtilities
{
    public required TemplateCompiler Compiler { get; init; }
}

public abstract class TemplatePlugin
{
    /// <summary>
    /// The content type this plugin handles.
    /// </summary>
    public abstract string ContentType { get; }

    protected PluginUtilities Utilities { get; private set; } = null!;

    /// <summary>
    /// Called by the TemplateHandler at runtime.
    /// </summary>
    public virtual void SetUtilities(PluginUtilities utilities)
    {
        Utilities = utilities;
    }

    /// <summary>
    /// This method is called for each self-closing tag.
    /// It should implement the specific document manipulation required by the tag.
    /// </summary>
    public virtual Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// This method is called for each container tag. It should implement the
    /// specific document manipulation required by the tag.
    /// </summary>
    /// <param name="tags">
    /// All tags between the opening tag and closing tag (inclusive,
    /// i.e. tags[0] is the opening tag and the last item in the tags list is
    /// the closing tag).
    /// </param>
    public virtual Task ContainerTagReplacementsAsync(IReadOnlyList<Tag> tags, ScopeData data, TemplateContext context)
    {
        return Task.CompletedTask;
    }
}
