using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Plugins.Loop.Strategy;
using Easy.Template.XCS.Utils;

namespace Easy.Template.XCS.Plugins.Loop;

public class LoopPlugin : TemplatePlugin
{
    public const string ContentTypeName = "loop";

    public override string ContentType => ContentTypeName;

    private readonly List<ILoopStrategy> loopStrategies = new()
    {
        new LoopParagraphStrategy(),
        new LoopTableColumnsStrategy(),
        new LoopTableRowsStrategy(),
        new LoopListStrategy(),
        new LoopContentStrategy() // the default strategy
    };

    public override async Task ContainerTagReplacementsAsync(IReadOnlyList<Tag> tags, ScopeData data, TemplateContext context)
    {
        var scopeData = data.GetScopeData();

        // non list value - treat as a boolean condition
        var isCondition = !TemplateData.IsList(scopeData);
        IReadOnlyList<object?> value;
        if (isCondition)
            value = TemplateData.IsTruthy(scopeData) ? new object?[] { new object() } : Array.Empty<object?>();
        else
            value = TemplateData.ToList(scopeData);

        // vars
        var firstTag = tags[0];
        var lastTag = tags[tags.Count - 1];

        if (firstTag is not TextNodeTag openTag)
            throw new TemplateSyntaxException($"Loop opening tag \"{firstTag.RawText}\" must be placed in a text node but was placed in {firstTag.Placement}");
        if (lastTag is not TextNodeTag closeTag)
            throw new TemplateSyntaxException($"Loop closing tag \"{lastTag.RawText}\" must be placed in a text node but was placed in {lastTag.Placement}");

        if (OfficeMarkup.Query.ContainingStructuredTagContentNode(openTag.XmlTextNode) != null)
            throw new TemplateSyntaxException($"Loop tag \"{openTag.RawText}\" cannot be placed inside a content control");
        if (OfficeMarkup.Query.ContainingStructuredTagContentNode(closeTag.XmlTextNode) != null)
            throw new TemplateSyntaxException($"Loop tag \"{closeTag.RawText}\" cannot be placed inside a content control");

        // select the suitable strategy
        var loopStrategy = loopStrategies.Find(strategy => strategy.IsApplicable(openTag, closeTag, isCondition))
            ?? throw new InternalException($"No loop strategy found for tag '{openTag.RawText}'.");

        // prepare to loop
        var splitResult = loopStrategy.SplitBefore(openTag, closeTag);

        // repeat (loop) the content
        var repeatedNodes = Repeat(splitResult.NodesToRepeat, value.Count, context);

        // recursive compilation
        // (this step can be optimized in the future if we'll keep track of the
        // path to each token and use that to create new tokens instead of
        // search through the text again)
        var compiledNodes = await CompileAsync(isCondition, repeatedNodes, data, context).ConfigureAwait(false);

        // merge back to the document
        loopStrategy.MergeBack(compiledNodes, splitResult.FirstNode, splitResult.LastNode);
    }

    private List<List<OpenXmlElement>> Repeat(List<OpenXmlElement> nodes, int times, TemplateContext context)
    {
        var allResults = new List<List<OpenXmlElement>>();
        if (nodes.Count == 0 || times == 0)
            return allResults;

        // Bookmark IDs must be unique in the document, so cloned bookmarks get
        // new IDs. We locate the bookmarks once, on the source nodes, and
        // address them by their index path in each clone (walking the
        // descendants of every clone is expensive).
        var bookmarkPaths = FindBookmarkPaths(nodes);

        for (var i = 0; i < times; i++)
        {
            var curResult = nodes.Select(node => node.CloneNode(true)).ToList();
            if (bookmarkPaths.Count > 0)
                RenumberBookmarks(curResult, bookmarkPaths, context);
            allResults.Add(curResult);
        }

        return allResults;
    }

    private sealed class LoopPluginContext
    {
        /// <summary>
        /// Last bookmark ID for each OOXML part. Key is the OOXML part uri.
        /// </summary>
        public Dictionary<string, int> LastBookmarkId { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// The index path (node index, then child indexes) of each bookmark start / end element.
    /// </summary>
    private static List<int[]> FindBookmarkPaths(List<OpenXmlElement> nodes)
    {
        var paths = new List<int[]>();
        for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
        {
            var node = nodes[nodeIndex];
            foreach (var bookmark in node.Descendants().Prepend(node))
            {
                if (bookmark is not (BookmarkStart or BookmarkEnd))
                    continue;

                var path = new List<int>();
                var current = bookmark;
                while (current != node)
                {
                    var parent = current.Parent!;
                    var index = 0;
                    for (var sibling = parent.FirstChild; sibling != current; sibling = sibling!.NextSibling())
                        index++;
                    path.Add(index);
                    current = parent;
                }
                path.Add(nodeIndex);
                path.Reverse();
                paths.Add(path.ToArray());
            }
        }
        return paths;
    }

    private void RenumberBookmarks(List<OpenXmlElement> clonedNodes, List<int[]> bookmarkPaths, TemplateContext context)
    {
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in bookmarkPaths)
        {
            OpenXmlElement bookmark = clonedNodes[path[0]];
            for (var i = 1; i < path.Length; i++)
                bookmark = bookmark.ChildElements[path[i]];

            StringValue? id = bookmark switch
            {
                BookmarkStart start => start.Id,
                BookmarkEnd end => end.Id,
                _ => null
            };
            if (id?.Value is null)
                continue;

            if (!idMap.TryGetValue(id.Value, out var newId))
            {
                newId = NextBookmarkId(context).ToString(CultureInfo.InvariantCulture);
                idMap[id.Value] = newId;
            }

            switch (bookmark)
            {
                case BookmarkStart start:
                    start.Id = newId;
                    break;
                case BookmarkEnd end:
                    end.Id = newId;
                    break;
            }
        }
    }

    private int NextBookmarkId(TemplateContext context)
    {
        if (!context.PluginContext.TryGetValue(ContentType, out var rawPluginContext))
        {
            rawPluginContext = new LoopPluginContext();
            context.PluginContext[ContentType] = rawPluginContext;
        }
        var pluginContext = (LoopPluginContext)rawPluginContext;
        var lastIdKey = context.CurrentPart.Uri.OriginalString;

        if (!pluginContext.LastBookmarkId.TryGetValue(lastIdKey, out var lastId))
        {
            // start counting from the current max
            lastId = 0;
            var partRoot = context.CurrentPart.RootElement;
            if (partRoot != null)
            {
                foreach (var start in partRoot.Descendants<BookmarkStart>())
                {
                    if (int.TryParse(start.Id?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var existingId) && existingId > lastId)
                        lastId = existingId;
                }
            }
        }

        lastId++;
        pluginContext.LastBookmarkId[lastIdKey] = lastId;
        return lastId;
    }

    private async Task<List<List<OpenXmlElement>>> CompileAsync(bool isCondition, List<List<OpenXmlElement>> nodeGroups, ScopeData data, TemplateContext context)
    {
        var compiledNodeGroups = new List<List<OpenXmlElement>>();

        // compile each node group with it's relevant data
        for (var i = 0; i < nodeGroups.Count; i++)
        {
            // create dummy root node
            var curNodes = nodeGroups[i];
            var dummyRootNode = new OpenXmlUnknownElement("dummyRootNode");
            foreach (var node in curNodes)
                dummyRootNode.AppendChild(node);

            // compile the new root
            var conditionTag = UpdatePathBefore(isCondition, data, i);
            await Utilities.Compiler.CompileAsync(dummyRootNode, data, context).ConfigureAwait(false);
            UpdatePathAfter(isCondition, data, conditionTag);

            // disconnect from dummy root
            var curResult = new List<OpenXmlElement>();
            while (dummyRootNode.FirstChild != null)
            {
                var child = dummyRootNode.FirstChild;
                child.Remove();
                curResult.Add(child);
            }
            compiledNodeGroups.Add(curResult);
        }

        return compiledNodeGroups;
    }

    private static PathPart? UpdatePathBefore(bool isCondition, ScopeData data, int groupIndex)
    {
        // if it's a condition - don't go deeper in the path
        // (so we need to extract the already pushed condition tag)
        if (isCondition)
        {
            if (groupIndex > 0)
            {
                // should never happen - conditions should have at most one (synthetic) child...
                throw new InternalException($"Unexpected group index {groupIndex} for boolean condition at path \"{data.PathString()}\".");
            }
            return data.PathPop();
        }

        // else, it's a list - push the current index
        data.PathPush(groupIndex);
        return null;
    }

    private static void UpdatePathAfter(bool isCondition, ScopeData data, PathPart? conditionTag)
    {
        // reverse the "before" path operation
        if (isCondition)
            data.PathPush(conditionTag!);
        else
            data.PathPop();
    }
}
