namespace Easy.Template.XCS.Plugins.Link;

public class LinkContent : PluginContent
{
    public const string ContentTypeName = "link";

    public override string ContentType => ContentTypeName;

    /// <summary>
    /// The link text. If not specified the <see cref="Target"/> property will be used.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The link target (url).
    /// </summary>
    public string? Target { get; set; }

    public string? Tooltip { get; set; }

    public LinkContent()
    {
    }

    public LinkContent(string target, string? text = null, string? tooltip = null)
    {
        Target = target;
        Text = text;
        Tooltip = tooltip;
    }
}
