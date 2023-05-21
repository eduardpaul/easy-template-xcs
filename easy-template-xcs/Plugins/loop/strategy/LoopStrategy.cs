using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Tag = EasyTemplateXCS.Compilation.Tag;

namespace EasyTemplateXCS.Plugins.LoopPlugin;

public class LoopListStrategy : ILoopStrategy
{
    private PluginUtilities utilities;

    public void SetUtilities(PluginUtilities utilities)
    {
        this.utilities = utilities;
    }

    public bool IsApplicable(Tag openTag, Tag closeTag)
    {
        var containingParagraph = DocParserHelpers.FirstParentOfType<Paragraph>(openTag.XmlTextNode);
        return DocParserHelpers.IsListParagraph(containingParagraph);
    }

    public SplitBeforeResult SplitBefore(Tag openTag, Tag closeTag)
    {
        var firstParagraph = DocParserHelpers.FirstParentOfType<Paragraph>(openTag.XmlTextNode);
        var lastParagraph = DocParserHelpers.FirstParentOfType<Paragraph>(closeTag.XmlTextNode);
        var paragraphsToRepeat = DocParserHelpers.SiblingsInRange(firstParagraph, lastParagraph);

        // remove the loop tags
        openTag.XmlTextNode.Parent.RemoveChild(openTag.XmlTextNode);
        closeTag.XmlTextNode.Parent.RemoveChild(closeTag.XmlTextNode);

        return new SplitBeforeResult
        {
            FirstNode = firstParagraph,
            NodesToRepeat = paragraphsToRepeat,
            LastNode = lastParagraph
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> compiledNodes, OpenXmlElement firstParagraph, OpenXmlElement lastParagraphs)
    {
        foreach (var curParagraphsGroup in compiledNodes)
            foreach (var paragraph in curParagraphsGroup)
                paragraph.InsertAfterSelf(lastParagraphs);

        // remove the old paragraphs
        firstParagraph.Parent.RemoveChild(firstParagraph);
        if (firstParagraph != lastParagraphs)
            lastParagraphs.Parent.RemoveChild(lastParagraphs);
    }
}