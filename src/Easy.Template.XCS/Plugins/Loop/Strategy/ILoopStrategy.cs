using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Plugins.Loop.Strategy;

public interface ILoopStrategy
{
    bool IsApplicable(TextNodeTag openTag, TextNodeTag closeTag, bool isCondition);

    SplitBeforeResult SplitBefore(TextNodeTag openTag, TextNodeTag closeTag);

    void MergeBack(List<List<OpenXmlElement>> compiledNodes, OpenXmlElement firstNode, OpenXmlElement lastNode);
}

public sealed class SplitBeforeResult
{
    public required OpenXmlElement FirstNode { get; init; }
    public required List<OpenXmlElement> NodesToRepeat { get; init; }
    public required OpenXmlElement LastNode { get; init; }
}
