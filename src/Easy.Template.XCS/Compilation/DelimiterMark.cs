using DocumentFormat.OpenXml.Wordprocessing;

namespace Easy.Template.XCS.Compilation;

public class DelimiterMark
{
    public Text XmlTextNode { get; set; }
    public int Index { get; set; }
    public bool IsOpen { get; set; }
}
