using System.Text.RegularExpressions;

namespace Easy.Template.XCS.Compilation;

public static class TagUtils
{
    /// <summary>
    /// Creates a regex that matches a single tag (including its optional
    /// options section) using the specified delimiters.
    /// </summary>
    public static Regex TagRegex(XCS.Delimiters delimiters)
    {
        var tagOptionsPattern = $"{Regex.Escape(delimiters.TagOptionsStart)}(?<tagOptions>.*?){Regex.Escape(delimiters.TagOptionsEnd)}";
        var tagPattern = $"{Regex.Escape(delimiters.TagStart)}(?<tagName>.*?)(?:\\s*{tagOptionsPattern})?\\s*{Regex.Escape(delimiters.TagEnd)}";
        return new Regex(tagPattern, RegexOptions.Multiline | RegexOptions.CultureInvariant);
    }
}
