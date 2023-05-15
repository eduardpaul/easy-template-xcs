using DocumentFormat.OpenXml;
using EasyTemplateXCS.Compilation;

namespace EasyTemplateXCS.Plugins.LoopPlugin;

public interface ILoopStrategy
{
    void SetUtilities(PluginUtilities utilities);
    bool IsApplicable(Tag openTag, Tag closeTag);
    SplitBeforeResult SplitBefore(Tag openTag, Tag closeTag);
    void MergeBack(OpenXmlElement[][] compiledNodes, OpenXmlElement firstNode, OpenXmlElement lastNode);
}

public class SplitBeforeResult
{
    public OpenXmlElement FirstNode { get; set; }
    public OpenXmlElement[] NodesToRepeat { get; set; }
    public OpenXmlElement LastNode { get; set; }
}