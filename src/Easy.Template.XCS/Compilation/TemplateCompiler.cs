using DocumentFormat.OpenXml;
using Easy.Template.XCS.Plugins;

namespace Easy.Template.XCS.Compilation;

public class TemplateCompilerOptions
{
    public string DefaultContentType { get; set; }
    public string ContainerContentType { get; set; }
    public bool SkipEmptyTags { get; set; }
}

/**
 * The TemplateCompiler works roughly the same way as a source code compiler.
 * It's main steps are:
 *
 * 1. find delimiters (lexical analysis) :: (Document) => DelimiterMark[]
 * 2. extract tags (syntax analysis) :: (DelimiterMark[]) => Tag[]
 * 3. perform document replace (code generation) :: (Tag[], data) => Document*
 *
 * see: https://en.wikipedia.org/wiki/Compiler
 */
public class TemplateCompiler
{
    private readonly Dictionary<string, TemplatePlugin> pluginsLookup;
    private readonly DelimiterSearcher delimiterSearcher;
    private readonly TagParser tagParser;
    private readonly TemplateCompilerOptions options;

    public TemplateCompiler(
        DelimiterSearcher delimiterSearcher,
        TagParser tagParser,
        List<TemplatePlugin> plugins,
        TemplateCompilerOptions options
    )
    {
        this.delimiterSearcher = delimiterSearcher;
        this.tagParser = tagParser;
        this.options = options;
        this.pluginsLookup = plugins.ToDictionary(p => p.ContentType);
    }

    /**
     * Compiles the template and performs the required replacements using the
     * specified data.
     */
    public async Task Compile(OpenXmlElement node, ScopeData data, TemplateContext context)
    {
        var tags = this.ParseTags(node);
        await this.DoTagReplacements(tags, data, context);
    }

    public List<Tag> ParseTags(OpenXmlElement node)
    {
        var delimiters = this.delimiterSearcher.FindDelimiters(node);
        var tags = this.tagParser.Parse(delimiters.ToArray()).ToList();
        return tags;
    }

    private async Task DoTagReplacements(List<Tag> tags, ScopeData data, TemplateContext context)
    {
        for (int tagIndex = 0; tagIndex < tags.Count; tagIndex++)
        {
            var tag = tags[tagIndex];
            data.PathPush(new PathPart() { Tag = tag });
            var contentType = this.DetectContentType(tag, data);
            this.pluginsLookup.TryGetValue(contentType, out var plugin);
            if (plugin == null)
            {
                throw new UnknownContentTypeException(
                    contentType,
                    tag.RawText,
                    data.PathString()
                );
            }

            if (tag.Disposition == TagDisposition.SelfClosed)
            {
                await this.SimpleTagReplacements(plugin, tag, data, context);
            }
            else if (tag.Disposition == TagDisposition.Open)
            {
                // get all tags between the open and close tags
                var closingTagIndex = this.FindCloseTagIndex(tagIndex, tag, tags);
                var scopeTags = tags.GetRange(tagIndex, closingTagIndex - tagIndex + 1);
                tagIndex = closingTagIndex;

                // replace container tag
                await plugin.ContainerTagReplacements(scopeTags, data, context);
            }

            data.PathPop();
        }
    }

    private string DetectContentType(Tag tag, ScopeData data)
    {
        // explicit content type
        var scopeData = data.GetScopeData();
        if (PluginContentExtensions.IsPluginContent(scopeData))
            return ((PluginContent)scopeData)._type;
        
        // implicit - loop
        if (tag.Disposition == TagDisposition.Open || tag.Disposition == TagDisposition.Close)
        {
            return this.options.ContainerContentType;
        }

        // implicit - text
        return this.options.DefaultContentType;
    }

    private async Task SimpleTagReplacements(TemplatePlugin plugin, Tag tag, ScopeData data, TemplateContext context)
    {
        if (this.options.SkipEmptyTags && string.IsNullOrEmpty(data.GetScopeData().ToString()))
            return;

        await plugin.SimpleTagReplacements(tag, data, context);
    }

    private int FindCloseTagIndex(int fromIndex, Tag openTag, List<Tag> tags)
    {
        var openTags = 0;
        for (int i = fromIndex; i < tags.Count; i++)
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
                {
                    return i;
                }

                if (openTags < 0)
                {
                    // As long as we don't change the input to
                    // this method (fromIndex in particular) this
                    // should never happen.
                    throw new UnopenedTagException(tag.Name);
                }

                continue;
            }
        }

        throw new UnclosedTagException(openTag.Name);
    }
}
