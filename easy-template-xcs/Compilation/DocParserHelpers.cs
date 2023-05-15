
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EasyTemplateXCS;

public static class DocParserHelpers
{
    public static void JoinTextNodesRange(Text from, Text to)
    {
        // find run nodes
        var firstRunNode = ContainingRunNode(from);
        var secondRunNode = ContainingRunNode(to);

        var paragraphNode = firstRunNode.Parent;
        if (secondRunNode.Parent != paragraphNode)
            throw new Exception("Can not join text nodes from separate paragraphs.");

        // find "word text nodes"
        var firstWordTextNode = ContainingTextNode(from);
        var secondWordTextNode = ContainingTextNode(to);
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
                curWordTextNode = FirstTextNodeChild(curRunNode);
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
                curRunNode = curRunNode.NextSibling();
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

    public static Text SplitTextNode(Text textNode, int splitIndex, bool addBefore)
    {
        Text firstXmlTextNode;
        Text secondXmlTextNode;

        // split nodes
        var wordTextNode = ContainingTextNode(textNode);
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

    private static OpenXmlElement ContainingRunNode(OpenXmlElement node)
    {
        while (node != null && !(node is Run))
            node = node.Parent;

        return node;
    }

    private static OpenXmlElement? ContainingTextNode(OpenXmlElement node)
    {
        while (node != null && !(node is Text))
            node = node.Parent;

        return node;
    }

    private static OpenXmlElement? FirstTextNodeChild(OpenXmlElement node)
    {
        if (!node.HasChildren)
            return null;

        return node?.ChildElements?.FirstOrDefault(n => n is Text);
    }

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
}