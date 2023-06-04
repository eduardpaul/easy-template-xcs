
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Easy.Template.XCS;

public static class DocParserHelpers
{
    /**
     * Move all text between the 'from' and 'to' nodes to the 'from' node.
     */
    public static void JoinTextNodesRange(Text from, Text to)
    {
        // find run nodes
        var firstRunNode = FirstParentOfType<Run>(from);
        var secondRunNode = FirstParentOfType<Run>(to);

        var paragraphNode = firstRunNode.Parent;
        if (secondRunNode.Parent != paragraphNode)
            throw new Exception("Can not join text nodes from separate paragraphs.");

        // find "word text nodes"
        var firstWordTextNode = FirstParentOfType<Text>(from);
        var secondWordTextNode = FirstParentOfType<Text>(to);
        var totalText = new List<string>();

        // iterate runs
        var curRunNode = firstRunNode;
        while (curRunNode != null)
        {
            // iterate text nodes
            OpenXmlElement curWordTextNode;
            if (curRunNode == firstRunNode)
            {
                curWordTextNode = firstWordTextNode;
            }
            else
            {
                curWordTextNode = FirstChildOfType<Text>(curRunNode);
            }

            while (curWordTextNode != null)
            {
                if (!(curWordTextNode is Text))
                {
                    continue;
                }

                // move text to first node
                var curXmlTextNode = LastTextChild(curWordTextNode);
                totalText.Add(curXmlTextNode.Text);

                // next text node
                var textToRemove = curWordTextNode;
                if (curWordTextNode == secondWordTextNode)
                {
                    curWordTextNode = null;
                }
                else
                {
                    curWordTextNode = curWordTextNode.NextSibling();
                }

                // remove current text node
                if (textToRemove != firstWordTextNode)
                {
                    textToRemove.Parent.RemoveChild(textToRemove);
                }
            }

            // next run
            var runToRemove = curRunNode;
            if (curRunNode == secondRunNode)
            {
                curRunNode = null;
            }
            else
            {
                curRunNode = curRunNode.NextSibling() as Run;
            }

            // remove current run
            if (!runToRemove.HasChildren)
            {
                runToRemove.Parent.RemoveChild(runToRemove);
            }
        }

        // set the text content
        var firstXmlTextNode = LastTextChild(firstWordTextNode);
        firstXmlTextNode.Text = string.Join("", totalText);
    }

    /**
     * Split the text node into two text nodes, each with it's own wrapping <w:t> node.
     * Returns the newly created text node.
     *
     * @param textNode
     * @param splitIndex
     * @param addBefore Should the new node be added before or after the original node.
     */
    public static Text SplitTextNode(Text textNode, int splitIndex, bool addBefore)
    {
        Text firstXmlTextNode;
        Text secondXmlTextNode;

        // split nodes
        var wordTextNode = FirstChildOfType<Text>(textNode);
        var newWordTextNode = wordTextNode.CloneNode(true);

        // set space preserve to prevent display differences after splitting
        // (otherwise if there was a space in the middle of the text node and it
        // is now at the beginning or end of the text node it will be ignored)
        wordTextNode.SetAttribute(new OpenXmlAttribute("space", "xml", "preserve"));
        newWordTextNode.SetAttribute(new OpenXmlAttribute("space", "xml", "preserve"));

        if (addBefore)
        {
            // insert new node before existing one
            wordTextNode.InsertBeforeSelf(newWordTextNode);

            firstXmlTextNode = LastTextChild(newWordTextNode);
            secondXmlTextNode = textNode;
        }
        else
        {
            // insert new node after existing one
            wordTextNode.InsertAfterSelf(newWordTextNode);

            firstXmlTextNode = textNode;
            secondXmlTextNode = LastTextChild(newWordTextNode);
        }

        // edit text
        var firstText = firstXmlTextNode.Text;
        var secondText = secondXmlTextNode.Text;
        firstXmlTextNode.Text = firstText.Substring(0, splitIndex);
        secondXmlTextNode.Text = secondText.Substring(splitIndex);

        return (addBefore ? firstXmlTextNode : secondXmlTextNode);
    }

    /**
     * Split the paragraph around the specified text node.
     *
     * @returns Two paragraphs - `left` and `right`. If the `removeTextNode` argument is
     * `false` then the original text node is the first text node of `right`.
     */
    public static (OpenXmlElement, OpenXmlElement) SplitParagraphByTextNode(OpenXmlElement paragraph, Text textNode, bool removeTextNode)
    {
        // input validation
        var containingParagraph = FirstParentOfType<Paragraph>(textNode);
        if (containingParagraph != paragraph)
            throw new Exception($"Node '{nameof(textNode)}' is not a descendant of '{nameof(paragraph)}'.");

        var runNode = FirstParentOfType<Run>(textNode);
        var wordTextNode = FirstParentOfType<Text>(textNode);

        // create run clone
        var leftRun = runNode.CloneNode(false);
        var rightRun = runNode;
        rightRun.InsertBeforeSelf(leftRun);

        // copy props from original run node (preserve style)
        var runProps = FirstChildOfType<RunProperties>(runNode);
        if (runProps != null)
        {
            var leftRunProps = runProps.CloneNode(true);
            leftRun.AppendChild(leftRunProps);
        }

        // move nodes from 'right' to 'left'
        int firstRunChildIndex = (runProps != null ? 1 : 0);
        var curChild = rightRun.ChildElements[firstRunChildIndex];
        while (curChild != wordTextNode)
        {
            curChild.Parent.RemoveChild(curChild);
            leftRun.AppendChild(curChild);
            curChild = rightRun.ChildElements[firstRunChildIndex];
        }

        // remove text node
        if (removeTextNode)
        {
            rightRun.ChildElements[firstRunChildIndex].Remove();
        }

        // create paragraph clone
        var leftPara = containingParagraph.CloneNode(false);
        var rightPara = containingParagraph;
        rightPara.InsertBeforeSelf(leftPara);

        // copy props from original paragraph (preserve style)
        var paragraphProps = FirstChildOfType<ParagraphProperties>(rightPara);
        if (paragraphProps != null)
        {
            var leftParagraphProps = paragraphProps.CloneNode(true);
            leftPara.AppendChild(leftParagraphProps);
        }

        // move nodes from 'right' to 'left'
        int firstParaChildIndex = (paragraphProps != null ? 1 : 0);
        curChild = rightPara.ChildElements[firstParaChildIndex];
        while (curChild != rightRun)
        {
            curChild.Parent.RemoveChild(curChild);
            leftPara.AppendChild(curChild);
            curChild = rightPara.ChildElements[firstParaChildIndex];
        }

        // clean paragraphs - remove empty runs
        if (IsEmptyRun(leftRun))
            leftRun.Parent.RemoveChild(leftRun);
        if (IsEmptyRun(rightRun))
            rightRun.Parent.RemoveChild(rightRun);

        return (leftPara, rightPara);
    }

    public static bool IsEmptyRun(OpenXmlElement node)
    {
        if (!(node is Run))
            throw new Exception($"Run node expected but '{node.LocalName}' received.");

        if (!node.HasChildren)
            return true;

        foreach (OpenXmlElement child in node.ChildElements)
        {
            if (child is RunProperties)
                continue;

            if (child is Text && IsEmptyTextNode(child))
                continue;

            return false;
        }

        return true;
    }

    public static bool IsEmptyTextNode(OpenXmlElement node)
    {
        if (!(node is Text text))
            throw new Exception($"Text node expected but '{node.LocalName}' received.");

        if (string.IsNullOrEmpty(text.Text))
            return true;

        return false;
    }

    public static T FirstParentOfType<T>(OpenXmlElement node) where T : OpenXmlElement
    {
        while (node != null && !(node is T))
            node = node.Parent;
        return (T)node;
    }

    public static T FirstChildOfType<T>(OpenXmlElement node) where T : OpenXmlElement
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (node is T)
            return node as T;
        if (!node.HasChildren)
            return null;
        return node?.ChildElements?.FirstOrDefault(n => n is T) as T;
    }

    /**
     * Returns all siblings between 'firstNode' and 'lastNode' inclusive.
     */
    public static List<OpenXmlElement> SiblingsInRange(OpenXmlElement firstNode, OpenXmlElement lastNode)
    {
        if (firstNode == null)
            throw new ArgumentNullException(nameof(firstNode));
        if (lastNode == null)
            throw new ArgumentNullException(nameof(lastNode));

        var range = new List<OpenXmlElement>();
        OpenXmlElement curNode = firstNode;
        while (curNode != null && curNode != lastNode)
        {
            range.Add(curNode);
            curNode = curNode.NextSibling();
        }

        if (curNode == null)
            throw new Exception("Nodes are not siblings.");

        range.Add(lastNode);
        return range;
    }

    public static bool IsListParagraph(OpenXmlElement paragraphNode)
    {
        var paragraphProperties = FirstChildOfType<ParagraphProperties>(paragraphNode);
        var listNumberProperties = FirstChildOfType<NumberingProperties>(paragraphProperties);
        return listNumberProperties != null;
    }

    /**
     * Gets the last direct child text node if it exists. Otherwise creates a
     * new text node, appends it to 'node' and return the newly created text
     * node.
     *
     * The function also makes sure the returned text node has a valid string
     * value.
     */
    private static Text LastTextChild(OpenXmlElement node)
    {
        if (node is Text)
            return (Text)node;

        // existing text nodes
        if (node.HasChildren)
        {
            var allTextNodes = node.ChildElements.Where(child => child is Text).Cast<Text>().ToList();
            if (allTextNodes.Any())
            {
                var lastTextNode = allTextNodes.Last();
                if (string.IsNullOrEmpty(lastTextNode.Text))
                {
                    lastTextNode.Text = "";
                }
                return lastTextNode;
            }
        }

        // create new text node
        var newTextNode = new Text();
        node.AppendChild(newTextNode);

        return newTextNode;
    }

    /**
     * Take all runs from 'second' and move them to 'first'.
     */
    public static void JoinParagraphs(OpenXmlElement first, OpenXmlElement second)
    {
        if (first == second)
            return;

        int childIndex = 0;
        while (second.ChildElements != null && childIndex < second.ChildElements.Count)
        {
            var curChild = second.ChildElements[childIndex];
            if (curChild is Run)
            {
                second.ChildElements[childIndex].Remove();
                first.AppendChild(curChild);
            }
            else
            {
                childIndex++;
            }
        }
    }

    /**
     * Remove sibling nodes between 'from' and 'to' excluding both.
     * Return the removed nodes.
     */
    public static List<OpenXmlElement> RemoveSiblings(OpenXmlElement from, OpenXmlElement to)
    {
        var removed = new List<OpenXmlElement>();
        if (from == to)
            return removed;

        //OpenXmlElement lastRemoved = null;
        from = from.NextSibling();
        while (from != to)
        {
            var removeMe = from;
            from = from.NextSibling();

            removeMe.Parent.RemoveChild(removeMe);
            removed.Add(removeMe);

            //if (lastRemoved != null)
            //     lastRemoved.NextSibling = removeMe;
            //lastRemoved = removeMe;
        }

        return removed.ToList();
    }
}