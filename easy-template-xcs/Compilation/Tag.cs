using DocumentFormat.OpenXml.Wordprocessing;

namespace EasyTemplateXCS.Compilation;

public enum TagDisposition
{
    Open,
    Close,
    SelfClosed
}

public class Tag
{
    public string Name { get; set; }
    public string RawText { get; set; }
    public TagDisposition Disposition { get; set; }
    public Text XmlTextNode { get; set; }
}

