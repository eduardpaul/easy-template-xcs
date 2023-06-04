using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Tag = Easy.Template.XCS.Compilation.Tag;

namespace Easy.Template.XCS.Plugins.LoopPlugin;

public class LoopParagraphStrategy : ILoopStrategy
{
    private PluginUtilities utilities;

    public void SetUtilities(PluginUtilities utilities)
    {
        this.utilities = utilities;
    }

    public bool IsApplicable(Tag openTag, Tag closeTag)
    {
        return true;
    }

    public SplitBeforeResult SplitBefore(Tag openTag, Tag closeTag)
    {
        // gather some info
        OpenXmlElement firstParagraph = DocParserHelpers.FirstParentOfType<Paragraph>(openTag.XmlTextNode);
        OpenXmlElement lastParagraph = DocParserHelpers.FirstParentOfType<Paragraph>(closeTag.XmlTextNode);
        bool areSame = (firstParagraph == lastParagraph);

        // split first paragraph
        var splitResult = DocParserHelpers.SplitParagraphByTextNode(firstParagraph, openTag.XmlTextNode, true);
        firstParagraph = splitResult.Item1;
        OpenXmlElement afterFirstParagraph = splitResult.Item2;
        if (areSame)
            lastParagraph = afterFirstParagraph;

        // split last paragraph
        splitResult = DocParserHelpers.SplitParagraphByTextNode(lastParagraph, closeTag.XmlTextNode, true);
        OpenXmlElement beforeLastParagraph = splitResult.Item1;
        lastParagraph = splitResult.Item2;
        if (areSame)
            afterFirstParagraph = beforeLastParagraph;

        // disconnect splitted paragraph from their parents
        afterFirstParagraph.Remove();
        if (!areSame)
            beforeLastParagraph.Remove();

        // extract all paragraphs in between
        List<OpenXmlElement> middleParagraphs;
        if (areSame)
        {
            middleParagraphs = new List<OpenXmlElement> { afterFirstParagraph };
        }
        else
        {
            List<OpenXmlElement> inBetween = DocParserHelpers.RemoveSiblings(firstParagraph, lastParagraph);
            middleParagraphs = new List<OpenXmlElement> { afterFirstParagraph }.Concat(inBetween).Concat(beforeLastParagraph).ToList();
        }

        return new SplitBeforeResult
        {
            FirstNode = firstParagraph,
            NodesToRepeat = middleParagraphs,
            LastNode = lastParagraph
        };
    }

    public void MergeBack(List<List<OpenXmlElement>> compiledNodes, OpenXmlElement firstParagraph, OpenXmlElement lastParagraph)
    {
        OpenXmlElement mergeTo = firstParagraph;
        foreach (List<OpenXmlElement> curParagraphsGroup in compiledNodes)
        {
            // merge first paragraphs
            DocParserHelpers.JoinParagraphs(mergeTo, curParagraphsGroup[0]);

            // add middle and last paragraphs to the original document
            for (int i = 1; i < curParagraphsGroup.Count; i++)
            {
                curParagraphsGroup[i].InsertBeforeSelf(lastParagraph);
                mergeTo = curParagraphsGroup[i];
            }
        }

        // merge last paragraph
        DocParserHelpers.JoinParagraphs(mergeTo, lastParagraph);

        // remove the old last paragraph (was merged into the new one)
        lastParagraph.Remove();
    }
}