using DocumentFormat.OpenXml.Packaging;

namespace Easy.Template.XCS.Compilation;

public sealed class TemplateContext
{
    public required WordprocessingDocument Docx { get; init; }

    /// <summary>
    /// The part (main document, header, footer) currently being processed.
    /// </summary>
    public OpenXmlPart CurrentPart { get; set; } = null!;

    /// <summary>
    /// Private context for plugins. Key is the plugin content type.
    /// </summary>
    public Dictionary<string, object> PluginContext { get; } = new(StringComparer.Ordinal);

    public required TemplateOptions Options { get; init; }
}

public sealed class TemplateOptions
{
    public int MaxXmlDepth { get; init; }
}
