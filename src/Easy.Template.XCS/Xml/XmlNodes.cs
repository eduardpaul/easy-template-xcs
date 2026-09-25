using DocumentFormat.OpenXml;

namespace Easy.Template.XCS.Xml;

/// <summary>
/// Generic OpenXml element tree utilities (not specific to Wordprocessing markup).
/// </summary>
public static class XmlNodes
{
    /// <summary>
    /// Search upwards (including the node itself) for the first node matching the predicate.
    /// </summary>
    public static OpenXmlElement? FindParent(OpenXmlElement? node, Func<OpenXmlElement, bool> predicate)
    {
        while (node != null)
        {
            if (predicate(node))
                return node;
            node = node.Parent;
        }
        return null;
    }

    /// <summary>
    /// Search upwards (including the node itself) for the first node of the specified type.
    /// </summary>
    public static T? FindParent<T>(OpenXmlElement? node) where T : OpenXmlElement
    {
        while (node != null)
        {
            if (node is T typed)
                return typed;
            node = node.Parent;
        }
        return null;
    }

    public static OpenXmlElement? FindChild(OpenXmlElement? node, Func<OpenXmlElement, bool> predicate)
    {
        if (node is null || !node.HasChildren)
            return null;

        foreach (var child in node.ChildElements)
        {
            if (predicate(child))
                return child;
        }
        return null;
    }

    /// <summary>
    /// Returns all siblings between 'firstNode' and 'lastNode' inclusive.
    /// </summary>
    public static List<OpenXmlElement> SiblingsInRange(OpenXmlElement firstNode, OpenXmlElement lastNode)
    {
        ArgumentNullException.ThrowIfNull(firstNode);
        ArgumentNullException.ThrowIfNull(lastNode);

        var range = new List<OpenXmlElement>();
        var curNode = firstNode;
        while (curNode != null && curNode != lastNode)
        {
            range.Add(curNode);
            curNode = curNode.NextSibling();
        }

        if (curNode is null)
            throw new ArgumentException("Nodes are not siblings.");

        range.Add(lastNode);
        return range;
    }

    /// <summary>
    /// Remove the node from its parent (if it has one).
    /// </summary>
    public static void Remove(OpenXmlElement node)
    {
        if (node.Parent != null)
            node.Remove();
    }

    /// <summary>
    /// Remove sibling nodes between 'from' and 'to' excluding both.
    /// Returns the removed nodes.
    /// </summary>
    public static List<OpenXmlElement> RemoveSiblings(OpenXmlElement from, OpenXmlElement to)
    {
        var removed = new List<OpenXmlElement>();
        if (from == to)
            return removed;

        var current = from.NextSibling();
        while (current != null && current != to)
        {
            var removeMe = current;
            current = current.NextSibling();
            removeMe.Remove();
            removed.Add(removeMe);
        }

        return removed;
    }

    public static void InsertBefore(OpenXmlElement newNode, OpenXmlElement referenceNode)
    {
        Remove(newNode);
        referenceNode.InsertBeforeSelf(newNode);
    }

    public static void InsertAfter(OpenXmlElement newNode, OpenXmlElement referenceNode)
    {
        Remove(newNode);
        referenceNode.InsertAfterSelf(newNode);
    }

    public static void AppendChild(OpenXmlElement parent, OpenXmlElement child)
    {
        Remove(child);
        parent.AppendChild(child);
    }

    /// <summary>
    /// Split the parent node into two nodes around the specified child.
    /// Returns [left, right] where 'right' is the original node.
    /// </summary>
    public static (OpenXmlElement Left, OpenXmlElement Right) SplitByChild(OpenXmlElement parent, OpenXmlElement child, bool removeChild)
    {
        if (child.Parent != parent)
            throw new ArgumentException("Node 'child' is not a direct child of 'parent'.");

        // create childless clone 'left'
        var left = parent.CloneNode(false);
        if (parent.Parent != null)
            parent.InsertBeforeSelf(left);
        var right = parent;

        // move nodes from 'right' to 'left'
        var curChild = right.FirstChild;
        while (curChild != null && curChild != child)
        {
            curChild.Remove();
            left.AppendChild(curChild);
            curChild = right.FirstChild;
        }

        // remove child
        if (removeChild)
            child.Remove();

        return (left, right);
    }

    /// <summary>
    /// Get the value of an attribute by its local name and namespace uri, or null if it does not exist.
    /// </summary>
    public static string? GetAttributeValue(OpenXmlElement node, string localName, string namespaceUri = "")
    {
        foreach (var attribute in node.GetAttributes())
        {
            if (attribute.LocalName == localName && (attribute.NamespaceUri ?? "") == namespaceUri)
                return attribute.Value;
        }
        return null;
    }

    public static void SetAttributeValue(OpenXmlElement node, string localName, string? value, string namespaceUri = "", string prefix = "")
    {
        if (value is null)
        {
            RemoveAttribute(node, localName, namespaceUri);
            return;
        }

        node.SetAttribute(new OpenXmlAttribute(prefix, localName, namespaceUri, value));
    }

    public static void RemoveAttribute(OpenXmlElement node, string localName, string namespaceUri = "")
    {
        if (GetAttributeValue(node, localName, namespaceUri) != null)
            node.RemoveAttribute(localName, namespaceUri);
    }
}
