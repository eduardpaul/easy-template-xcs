using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Xml;

namespace Easy.Template.XCS.Compilation.Delimiters;

public class DelimiterSearcher
{
    private readonly int maxXmlDepth;
    private readonly XCS.Delimiters delimiters;
    private readonly Regex tagRegex;

    public DelimiterSearcher(XCS.Delimiters delimiters, int maxXmlDepth)
    {
        if (delimiters is null)
            throw new InternalArgumentMissingException(nameof(delimiters));
        if (maxXmlDepth <= 0)
            throw new InternalArgumentMissingException(nameof(maxXmlDepth));

        this.delimiters = delimiters;
        this.maxXmlDepth = maxXmlDepth;
        tagRegex = TagUtils.TagRegex(delimiters);
    }

    public List<DelimiterMark> FindDelimiters(OpenXmlElement node)
    {
        var delimiterMarks = new List<DelimiterMark>();
        var it = new XmlTreeIterator(node, maxXmlDepth);

        var attributeSearcher = new AttributesDelimiterSearcher(delimiters, tagRegex);
        var textSearcher = new TextNodesDelimiterSearcher(delimiters.TagStart, delimiters.TagEnd);

        while (it.Node != null)
        {
            attributeSearcher.ProcessNode(it, delimiterMarks);
            textSearcher.ProcessNode(it, delimiterMarks);
            it.Next();
        }

        return delimiterMarks;
    }
}
