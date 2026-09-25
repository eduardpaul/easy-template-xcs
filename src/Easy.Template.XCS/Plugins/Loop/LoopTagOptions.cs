using Easy.Template.XCS.Compilation;

namespace Easy.Template.XCS.Plugins.Loop;

public static class LoopOver
{
    /// <summary>
    /// Loop over the entire table row.
    /// </summary>
    public const string Row = "row";

    /// <summary>
    /// Loop over the entire table column.
    /// </summary>
    public const string Column = "column";

    /// <summary>
    /// Loop over the entire paragraph.
    /// </summary>
    public const string Paragraph = "paragraph";

    /// <summary>
    /// Loop over the content enclosed between the opening and closing tag.
    /// </summary>
    public const string Content = "content";
}

public static class LoopTagOptions
{
    public const string LoopOverKey = "loopOver";

    /// <summary>
    /// Get the 'loopOver' option of the tag, or null if not specified.
    /// </summary>
    public static string? GetLoopOver(Tag tag)
    {
        if (tag.Options is null)
            return null;

        if (!tag.Options.TryGetValue(LoopOverKey, out var value))
            return null;

        return value?.ToString();
    }
}
