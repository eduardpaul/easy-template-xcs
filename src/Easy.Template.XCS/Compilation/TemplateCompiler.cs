using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation.Delimiters;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Plugins;
using Easy.Template.XCS.Utils;

namespace Easy.Template.XCS.Compilation;

public sealed class TemplateCompilerOptions
{
    public required string DefaultContentType { get; init; }
    public required string ContainerContentType { get; init; }
    public bool SkipEmptyTags { get; init; }
}

/// <summary>
/// The TemplateCompiler works roughly the same way as a source code compiler.
/// It's main steps are:
///
/// 1. find delimiters (lexical analysis) :: (Document) => DelimiterMark[]
/// 2. extract tags (syntax analysis) :: (DelimiterMark[]) => Tag[]
/// 3. perform document replace (code generation) :: (Tag[], data) => Document*
///
/// see: https://en.wikipedia.org/wiki/Compiler
/// </summary>
public class TemplateCompiler
{
    private readonly Dictionary<string, TemplatePlugin> pluginsLookup;
    private readonly DelimiterSearcher delimiterSearcher;
    private readonly TagParser tagParser;
    private readonly TemplateCompilerOptions options;

    public TemplateCompiler(
        DelimiterSearcher delimiterSearcher,
        TagParser tagParser,
        IEnumerable<TemplatePlugin> plugins,
        TemplateCompilerOptions options)
    {
        this.delimiterSearcher = delimiterSearcher;
        this.tagParser = tagParser;
        this.options = options;
        pluginsLookup = new Dictionary<string, TemplatePlugin>(StringComparer.Ordinal);
        foreach (var plugin in plugins)
            pluginsLookup[plugin.ContentType] = plugin;
    }

    /// <summary>
    /// Compiles the template and performs the required replacements using the
    /// specified data.
    /// </summary>
    public async Task CompileAsync(OpenXmlElement node, ScopeData data, TemplateContext context)
    {
        var tags = ParseTags(node);
        await DoTagReplacementsAsync(tags, data, context).ConfigureAwait(false);
    }

    public List<Tag> ParseTags(OpenXmlElement node)
    {
        var delimiters = delimiterSearcher.FindDelimiters(node);
        return tagParser.Parse(delimiters);
    }

    //
    // private methods
    //

    private async Task DoTagReplacementsAsync(List<Tag> tags, ScopeData data, TemplateContext context)
    {
        for (var tagIndex = 0; tagIndex < tags.Count; tagIndex++)
        {
            var tag = tags[tagIndex];
            data.PathPush(tag);
            var contentType = DetectContentType(tag, data);
            if (!pluginsLookup.TryGetValue(contentType, out var plugin))
                throw new UnknownContentTypeException(contentType, tag.RawText, data.PathString());

            if (tag.Disposition == TagDisposition.SelfClosed)
            {
                await SimpleTagReplacementsAsync(plugin, tag, data, context).ConfigureAwait(false);
            }
            else if (tag.Disposition == TagDisposition.Open)
            {
                // get all tags between the open and close tags
                var closingTagIndex = FindCloseTagIndex(tagIndex, tag, tags);
                var scopeTags = tags.GetRange(tagIndex, closingTagIndex - tagIndex + 1);
                tagIndex = closingTagIndex;

                // replace container tag
                await plugin.ContainerTagReplacementsAsync(scopeTags, data, context).ConfigureAwait(false);
            }

            data.PathPop();
        }
    }

    private string DetectContentType(Tag tag, ScopeData data)
    {
        // explicit content type
        var scopeData = data.GetScopeData();
        var pluginContentType = TemplateData.GetPluginContentType(scopeData);
        if (pluginContentType != null)
            return pluginContentType;

        // implicit - loop
        if (tag.Disposition is TagDisposition.Open or TagDisposition.Close)
            return options.ContainerContentType;

        // implicit - text
        return options.DefaultContentType;
    }

    private async Task SimpleTagReplacementsAsync(TemplatePlugin plugin, Tag tag, ScopeData data, TemplateContext context)
    {
        if (options.SkipEmptyTags && TemplateData.StringValue(data.GetScopeData()).Length == 0)
            return;

        await plugin.SimpleTagReplacementsAsync(tag, data, context).ConfigureAwait(false);
    }

    private static int FindCloseTagIndex(int fromIndex, Tag openTag, List<Tag> tags)
    {
        var openTags = 0;
        for (var i = fromIndex; i < tags.Count; i++)
        {
            var tag = tags[i];
            if (tag.Disposition == TagDisposition.Open)
            {
                openTags++;
                continue;
            }

            if (tag.Disposition == TagDisposition.Close)
            {
                openTags--;
                if (openTags == 0)
                    return i;

                if (openTags < 0)
                {
                    // As long as we don't change the input to
                    // this method (fromIndex in particular) this
                    // should never happen.
                    throw new UnopenedTagException(tag.Name, tag.RawText);
                }
            }
        }

        throw new UnclosedTagException(openTag.Name, openTag.RawText);
    }
}
