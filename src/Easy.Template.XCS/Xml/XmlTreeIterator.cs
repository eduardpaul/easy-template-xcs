using DocumentFormat.OpenXml;
using Easy.Template.XCS.Errors;

namespace Easy.Template.XCS.Xml;

/// <summary>
/// Depth-first, document order iterator over an OpenXml element tree.
/// </summary>
public class XmlTreeIterator
{
    private readonly XmlDepthTracker depthTracker;

    /// <summary>
    /// The current node. Null once the iteration is complete.
    /// </summary>
    public OpenXmlElement? Node { get; private set; }

    public XmlTreeIterator(OpenXmlElement initial, int maxDepth)
    {
        if (initial is null)
            throw new InternalException("Initial node is required");
        if (maxDepth <= 0)
            throw new InternalException("Max depth is required");

        Node = initial;
        depthTracker = new XmlDepthTracker(maxDepth);
    }

    public OpenXmlElement? Next()
    {
        if (Node is null)
            return null;

        Node = FindNextNode(Node);
        return Node;
    }

    public void SetCurrent(OpenXmlElement node)
    {
        Node = node;
    }

    private OpenXmlElement? FindNextNode(OpenXmlElement node)
    {
        // children
        if (node.HasChildren && node.FirstChild != null)
        {
            depthTracker.Increment();
            return node.FirstChild;
        }

        // siblings
        var sibling = node.NextSibling();
        if (sibling != null)
            return sibling;

        // parent sibling
        while (node.Parent != null)
        {
            var parentSibling = node.Parent.NextSibling();
            if (parentSibling != null)
            {
                depthTracker.Decrement();
                return parentSibling;
            }

            // go up
            depthTracker.Decrement();
            node = node.Parent;
        }

        return null;
    }
}
