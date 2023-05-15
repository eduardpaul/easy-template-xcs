using DocumentFormat.OpenXml.Wordprocessing;

namespace EasyTemplateXCS.Compilation;

public class DelimiterMark
{
    public Text XmlTextNode { get; set; }
    public int Index { get; set; }
    public bool IsOpen { get; set; }
}
