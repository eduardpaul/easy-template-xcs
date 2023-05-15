using DocumentFormat.OpenXml.Packaging;
using EasyTemplateXCS.Compilation;

namespace EasyTemplateXCS;

public class TemplateHandler
{
    private readonly TemplateCompiler compiler;
    private readonly TemplateHandlerOptions options;

    public TemplateHandler(TemplateHandlerOptions options = null)
    {
        this.options = new TemplateHandlerOptions(options);

        var delimiterSearcher = new DelimiterSearcher();
        delimiterSearcher.StartDelimiter = this.options.Delimiters.TagStart;
        delimiterSearcher.EndDelimiter = this.options.Delimiters.TagEnd;
        delimiterSearcher.MaxXmlDepth = this.options.MaxXmlDepth;

        var tagParser = new TagParser(this.options.Delimiters);

        this.compiler = new TemplateCompiler(
            delimiterSearcher,
            tagParser,
            this.options.Plugins,
            new TemplateCompilerOptions
            {
                SkipEmptyTags = this.options.SkipEmptyTags,
                DefaultContentType = this.options.DefaultContentType,
                ContainerContentType = this.options.ContainerContentType
            }
        );

        foreach (var plugin in this.options.Plugins)
        {
            plugin.SetUtilities(new Plugins.PluginUtilities
            {
                Compiler = this.compiler
            });
        }

        /* var extensionUtilities = new ExtensionUtilities
        {
            XmlParser = this.xmlParser,
            DocxParser = this.docxParser,
            TagParser = tagParser,
            Compiler = this.compiler
        };

        this.options.Extensions?.BeforeCompilation?.ForEach(extension =>
        {
            extension.SetUtilities(extensionUtilities);
        });

        this.options.Extensions?.AfterCompilation?.ForEach(extension =>
        {
            extension.SetUtilities(extensionUtilities);
        }); */
    }

    public async Task<Stream> Process(Stream templateFile, dynamic data)
    {
        var output = new MemoryStream();
        await templateFile.CopyToAsync(output);

        using WordprocessingDocument docx = WordprocessingDocument.Open(output, true);

        var scopeData = new ScopeData(data);
        scopeData.scopeDataResolver = this.options.ScopeDataResolver;
        var context = new TemplateContext
        {
            Docx = docx,
            CurrentPart = null
        };

        var contentParts = docx.GetAllParts();
        foreach (var part in contentParts)
        {
            context.CurrentPart = part;

            // await this.CallExtensions(this.options.Extensions?.BeforeCompilation, scopeData, context);

            await this.compiler.Compile(part.RootElement, scopeData, context);

            // await this.CallExtensions(this.options.Extensions?.AfterCompilation, scopeData, context);
        }

        return output;
    }

    public async Task<List<Tag>> ParseTags<T>(Stream templateFile) where T : OpenXmlPart
    {
        var output = new MemoryStream();
        await templateFile.CopyToAsync(output);

        using WordprocessingDocument docx = WordprocessingDocument.Open(output, true);

        var contentPart = docx.GetPartsOfType<T>().FirstOrDefault();
        if (contentPart == null)
            throw new Exception("Content part not found");

        return this.compiler.ParseTags(contentPart.RootElement);
    }

    public async Task<string> GetText<T>(Stream templateFile) where T : OpenXmlPart
    {
        var output = new MemoryStream();
        await templateFile.CopyToAsync(output);

        using WordprocessingDocument docx = WordprocessingDocument.Open(output, true);

        var contentPart = docx.GetPartsOfType<T>().FirstOrDefault();
        if (contentPart == null)
            throw new Exception("Content part not found");

        return contentPart.RootElement.InnerText;
    }

    public async Task<System.Xml.Linq.XElement> GetXml<T>(Stream templateFile) where T : OpenXmlPart
    {
        var output = new MemoryStream();
        await templateFile.CopyToAsync(output);

        using WordprocessingDocument docx = WordprocessingDocument.Open(output, true);

        var contentPart = docx.GetPartsOfType<T>().FirstOrDefault();
        if (contentPart == null)
            throw new Exception("Content part not found");

        return System.Xml.Linq.XElement.Parse(contentPart.RootElement.InnerXml);
    }
}
