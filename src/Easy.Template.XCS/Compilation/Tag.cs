using DocumentFormat.OpenXml.Wordprocessing;

namespace Easy.Template.XCS.Compilation;

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

