using System.Reflection;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Compilation.Delimiters;
using Easy.Template.XCS.Extensions;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Plugins;

namespace Easy.Template.XCS;

/// <summary>
/// Generates docx documents from templates.
/// </summary>
public class TemplateHandler
{
    /// <summary>
    /// Version number of the Easy.Template.XCS library.
    /// </summary>
    public static string Version { get; } =
        typeof(TemplateHandler).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(TemplateHandler).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private readonly TemplateCompiler compiler;
    private readonly TemplateHandlerOptions options;

    public TemplateHandler(TemplateHandlerOptions? options = null)
    {
        this.options = new TemplateHandlerOptions(options);
        var delimiters = this.options.Delimiters;

        //
        // This is the library's composition root
        //

        var delimiterSearcher = new DelimiterSearcher(delimiters, this.options.MaxXmlDepth);
        var tagParser = new TagParser(delimiters);

        compiler = new TemplateCompiler(
            delimiterSearcher,
            tagParser,
            this.options.Plugins,
            new TemplateCompilerOptions
            {
                SkipEmptyTags = this.options.SkipEmptyTags,
                DefaultContentType = this.options.DefaultContentType,
                ContainerContentType = this.options.ContainerContentType
            });

        foreach (var plugin in this.options.Plugins)
        {
            plugin.SetUtilities(new PluginUtilities
            {
                Compiler = compiler
            });
        }

        var extensionUtilities = new ExtensionUtilities
        {
            TagParser = tagParser,
            Compiler = compiler
        };

        foreach (var extension in this.options.Extensions.BeforeCompilation)
            extension.SetUtilities(extensionUtilities);

        foreach (var extension in this.options.Extensions.AfterCompilation)
            extension.SetUtilities(extensionUtilities);
    }

    //
    // Public methods
    //

    /// <summary>
    /// Process the template with the specified data and return the resulting document.
    /// </summary>
    /// <param name="templateFile">The template docx file.</param>
    /// <param name="data">
    /// The template data. Can be a dictionary, an anonymous object, a POCO, a
    /// <c>System.Text.Json</c> node or any combination of those.
    /// </param>
    public async Task<byte[]> ProcessAsync(byte[] templateFile, object? data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFile);

        using var input = new MemoryStream(templateFile, writable: false);
        using var output = await ProcessAsync(input, data, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }

    /// <summary>
    /// Process the template with the specified data and return the resulting document.
    /// The template stream is left untouched (and not disposed).
    /// </summary>
    /// <param name="templateFile">The template docx file.</param>
    /// <param name="data">
    /// The template data. Can be a dictionary, an anonymous object, a POCO, a
    /// <c>System.Text.Json</c> node or any combination of those.
    /// </param>
    public async Task<MemoryStream> ProcessAsync(Stream templateFile, object? data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFile);

        // load the docx file
        var (docx, backing) = await Docx.LoadAsync(templateFile, isEditable: true, cancellationToken).ConfigureAwait(false);
        try
        {
            using (docx)
            {
                // prepare context
                var scopeData = new ScopeData(data)
                {
                    Resolver = options.ScopeDataResolver
                };
                var context = new TemplateContext
                {
                    Docx = docx,
                    Options = new TemplateOptions
                    {
                        MaxXmlDepth = options.MaxXmlDepth
                    }
                };

                var contentParts = Docx.GetContentParts(docx);
                foreach (var part in contentParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    context.CurrentPart = part;

                    // extensions - before compilation
                    await CallExtensionsAsync(options.Extensions.BeforeCompilation, scopeData, context).ConfigureAwait(false);

                    // compilation (do replacements)
                    var xmlRoot = part.RootElement;
                    if (xmlRoot != null)
                        await compiler.CompileAsync(xmlRoot, scopeData, context).ConfigureAwait(false);

                    // extensions - after compilation
                    await CallExtensionsAsync(options.Extensions.AfterCompilation, scopeData, context).ConfigureAwait(false);
                }

                // export the result
                docx.Save();
            }

            backing.Position = 0;
            return backing;
        }
        catch
        {
            backing.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Parse all tags in the template (main document, headers and footers).
    /// </summary>
    public async Task<IReadOnlyList<Tag>> ParseTagsAsync(byte[] templateFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFile);

        using var input = new MemoryStream(templateFile, writable: false);
        return await ParseTagsAsync(input, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parse all tags in the template (main document, headers and footers).
    /// </summary>
    public async Task<IReadOnlyList<Tag>> ParseTagsAsync(Stream templateFile, CancellationToken cancellationToken = default)
    {
        var (docx, backing) = await Docx.LoadAsync(templateFile, isEditable: true, cancellationToken).ConfigureAwait(false);
        using (backing)
        using (docx)
        {
            var tags = new List<Tag>();
            foreach (var part in Docx.GetContentParts(docx))
            {
                var xmlRoot = part.RootElement;
                if (xmlRoot is null)
                    continue;

                tags.AddRange(compiler.ParseTags(xmlRoot));
            }

            return tags;
        }
    }

    /// <summary>
    /// Get the text content of one or more parts of the document.
    /// If more than one part exists, the concatenated text content of all parts is returned.
    /// If no matching parts are found, returns an empty string.
    /// </summary>
    /// <param name="docxFile">The docx file.</param>
    /// <param name="relType">
    /// The relationship type of the parts whose text content you want to retrieve.
    /// Defaults to <see cref="RelType.MainDocument"/>.
    /// </param>
    public async Task<string> GetTextAsync(byte[] docxFile, string relType = RelType.MainDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docxFile);

        using var input = new MemoryStream(docxFile, writable: false);
        return await GetTextAsync(input, relType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GetTextAsync(byte[], string, CancellationToken)"/>
    public async Task<string> GetTextAsync(Stream docxFile, string relType = RelType.MainDocument, CancellationToken cancellationToken = default)
    {
        var (docx, backing) = await Docx.LoadAsync(docxFile, isEditable: false, cancellationToken).ConfigureAwait(false);
        using (backing)
        using (docx)
        {
            var parts = Docx.GetParts(docx, relType);
            var partsText = parts.Select(p => p.RootElement?.InnerText ?? string.Empty);
            return string.Join("\n\n", partsText);
        }
    }

    /// <summary>
    /// Get the xml root of a single part of the document.
    /// If no matching part is found, returns null.
    /// </summary>
    /// <param name="docxFile">The docx file.</param>
    /// <param name="relType">
    /// The relationship type of the part whose xml root you want to retrieve.
    /// If more than one part exists, the first one is returned.
    /// Defaults to <see cref="RelType.MainDocument"/>.
    /// </param>
    public async Task<XElement?> GetXmlAsync(byte[] docxFile, string relType = RelType.MainDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docxFile);

        using var input = new MemoryStream(docxFile, writable: false);
        return await GetXmlAsync(input, relType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GetXmlAsync(byte[], string, CancellationToken)"/>
    public async Task<XElement?> GetXmlAsync(Stream docxFile, string relType = RelType.MainDocument, CancellationToken cancellationToken = default)
    {
        var (docx, backing) = await Docx.LoadAsync(docxFile, isEditable: false, cancellationToken).ConfigureAwait(false);
        using (backing)
        using (docx)
        {
            var part = Docx.GetParts(docx, relType).FirstOrDefault();
            var root = part?.RootElement;
            if (root is null)
                return null;

            return XElement.Parse(root.OuterXml);
        }
    }

    //
    // Private methods
    //

    private static async Task CallExtensionsAsync(IList<TemplateExtension> extensions, ScopeData scopeData, TemplateContext context)
    {
        foreach (var extension in extensions)
            await extension.ExecuteAsync(scopeData, context).ConfigureAwait(false);
    }
}
