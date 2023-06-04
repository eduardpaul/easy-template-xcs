namespace Easy.Template.XCS.Xml;

public class XmlDepthTracker
{
    private int depth = 0;
    private readonly int maxDepth;

    public XmlDepthTracker(int maxDepth)
    {
        this.maxDepth = maxDepth;
    }

    public void Increment()
    {
        depth++;
        if (depth > maxDepth)
        {
            throw new MaxXmlDepthException(maxDepth);
        }
    }

    public void Decrement()
    {
        depth--;
    }
}
