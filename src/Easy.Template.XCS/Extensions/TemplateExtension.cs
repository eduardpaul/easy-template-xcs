using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Extensions;

public sealed class ExtensionUtilities
{
    public required TemplateCompiler Compiler { get; init; }
    public required TagParser TagParser { get; init; }
}

/// <summary>
/// Extensions are executed before and/or after the compilation of each
/// content part (main document, headers, footers) and can be used to
/// implement custom document processing logic.
/// </summary>
public abstract class TemplateExtension
{
    protected ExtensionUtilities Utilities { get; private set; } = null!;

    /// <summary>
    /// Called by the TemplateHandler at runtime.
    /// </summary>
    public virtual void SetUtilities(ExtensionUtilities utilities)
    {
        Utilities = utilities;
    }

    public abstract Task ExecuteAsync(ScopeData data, TemplateContext context);
}

public sealed class ExtensionOptions
{
    public IList<TemplateExtension> BeforeCompilation { get; set; } = new List<TemplateExtension>();
    public IList<TemplateExtension> AfterCompilation { get; set; } = new List<TemplateExtension>();
}
