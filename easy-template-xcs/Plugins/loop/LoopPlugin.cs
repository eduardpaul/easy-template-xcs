using System.Collections;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using EasyTemplateXCS.Compilation;
using Tag = EasyTemplateXCS.Compilation.Tag;

namespace EasyTemplateXCS.Plugins.LoopPlugin;

public class LoopPlugin : TemplatePlugin
{
    public const string LOOP_CONTENT_TYPE = "loop";

    public override string ContentType => LOOP_CONTENT_TYPE;

    private readonly List<ILoopStrategy> loopStrategies = new List<ILoopStrategy>
        {
            new LoopTableStrategy(),
            new LoopListStrategy(),
            new LoopParagraphStrategy() // the default strategy
        };

    public override void SetUtilities(PluginUtilities utilities)
    {
        base.SetUtilities(utilities);
        foreach (var strategy in loopStrategies)
        {
            strategy.SetUtilities(utilities);
        }
    }

    public override async Task ContainerTagReplacements(List<Tag> tags, ScopeData data, TemplateContext context)
    {
        var value = data.GetScopeData();

        // Non array value - treat as a boolean condition.
        var isCondition = typeof(IEnumerable).IsAssignableFrom(data.GetType());
        if (isCondition)
        {
            if (value != null )
            {
                value = new List<object> { new Object() };
            }
            else
            {
                value = new List<object>();
            }
        }

        // vars
        var openTag = tags[0];
        var closeTag = tags.Last();

        // select the suitable strategy
        var loopStrategy = loopStrategies.FirstOrDefault(strategy => strategy.IsApplicable(openTag, closeTag));
        if (loopStrategy == null)
        {
            throw new Exception($"No loop strategy found for tag '{openTag.RawText}'.");
        }

        // prepare to loop
        var strategyResult = loopStrategy.SplitBefore(openTag, closeTag);
        //var (firstNode, nodesToRepeat, lastNode) = loopStrategy.SplitBefore(openTag, closeTag);

        // repeat (loop) the content
        var repeatedNodes = Repeat(strategyResult.NodesToRepeat, (value as IList).Count);

        // recursive compilation
        // (this step can be optimized in the future if we'll keep track of the
        // path to each token and use that to create new tokens instead of
        // search through the text again)
        var compiledNodes = await Compile(isCondition, repeatedNodes, data, context);

        // merge back to the document
        loopStrategy.MergeBack(compiledNodes, strategyResult.FirstNode, strategyResult.LastNode);
    }

    private List<List<OpenXmlElement>> Repeat(List<OpenXmlElement> nodes, int times)
    {
        if (!nodes.Any() || times == 0)
        {
            return new List<List<OpenXmlElement>>();
        }

        var allResults = new List<List<OpenXmlElement>>();

        for (var i = 0; i < times; i++)
        {
            var curResult = nodes.Select(node => (OpenXmlElement)node.Clone()).ToList();
            allResults.Add(curResult);
        }

        return allResults;
    }

    private async Task<List<List<OpenXmlElement>>> Compile(bool isCondition, List<List<OpenXmlElement>> nodeGroups, ScopeData data, TemplateContext context)
    {
        var compiledNodeGroups = new List<List<OpenXmlElement>>();

        // compile each node group with it's relevant data
        for (var i = 0; i < nodeGroups.Count; i++)
        {
            // create dummy root node
            var curNodes = nodeGroups[i];
            var dummyRootNode = new SdtContentBlock();
            foreach (var node in curNodes)
            {
                dummyRootNode.AddChild(node);
            }

            // compile the new root
            var conditionTag = UpdatePathBefore(isCondition, data, i);
            await Utilities.Compiler.Compile(dummyRootNode, data, context);
            UpdatePathAfter(isCondition, data, conditionTag);

            // disconnect from dummy root
            var curResult = new List<OpenXmlElement>();
            while (dummyRootNode.HasChildren)
            {
                var child = dummyRootNode.ChildElements.First();
                dummyRootNode.RemoveChild(child);
                curResult.Add(child);
            }
            compiledNodeGroups.Add(curResult);
        }

        return compiledNodeGroups;
    }

    private PathPart UpdatePathBefore(bool isCondition, ScopeData data, int groupIndex)
    {
        // if it's a condition - don't go deeper in the path
        // (so we need to extract the already pushed condition tag)
        if (isCondition)
        {
            if (groupIndex > 0)
            {
                // should never happen - conditions should have at most one (synthetic) child...
                throw new Exception($"Internal error: Unexpected group index {groupIndex} for boolean condition at path \"{data.PathString()}\".");
            }
            return data.PathPop();
        }

        // else, it's an array - push the current index
        data.PathPush(new PathPart() { Number = groupIndex });
        return null;
    }

    private void UpdatePathAfter(bool isCondition, ScopeData data, PathPart conditionTag)
    {
        // reverse the "before" path operation
        if (isCondition)
        {
            data.PathPush(conditionTag);
        }
        else
        {
            data.PathPop();
        }
    }
}