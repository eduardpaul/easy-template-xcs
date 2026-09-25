using System.Text.RegularExpressions;
using System.Xml.Linq;
using Easy.Template.XCS.Plugins.RawXml;

namespace Easy.Template.XCS.Test;

public static class TestUtils
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Files");
    private static readonly string ResDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Res");

    private static readonly string[] LoremWords =
    {
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do",
        "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim"
    };

    public static byte[] ReadFixture(string filename) => File.ReadAllBytes(Path.Combine(FixturesDir, filename));

    public static byte[] ReadResource(string filename) => File.ReadAllBytes(Path.Combine(ResDir, filename));

    public static string RemoveWhiteSpace(string text) => Regex.Replace(text, @"\s", "");

    public static string CompareableText(string text)
    {
        text = RemoveWhiteSpace(text);
        text = text.Replace('–', '-');
        text = text.Replace('“', '"').Replace('”', '"');
        text = text.Replace('’', '\'');
        return text;
    }

    public static string RandomWords(int count = 1)
    {
        var rnd = Random.Shared;
        return string.Join(" ", Enumerable.Range(0, count).Select(_ => LoremWords[rnd.Next(LoremWords.Length)]));
    }

    public static string RandomParagraphs(int count = 1)
    {
        return string.Join("\n", Enumerable.Range(0, count).Select(_ => RandomWords(20)));
    }

    /// <summary>
    /// Parse a wordprocessing xml fragment (using the common namespace prefixes) into an OpenXml element.
    /// </summary>
    public static T ParseXml<T>(string xml, bool removeWhiteSpace = true) where T : OpenXmlElement
    {
        if (removeWhiteSpace)
            xml = Regex.Replace(xml, @">\s+<", "><").Trim();

        var elements = RawXmlPlugin.ParseFragment(xml);
        return (T)elements.Single();
    }

    public static Text TextAt(OpenXmlElement root, params int[] path)
    {
        OpenXmlElement node = root;
        foreach (var index in path)
            node = node.ChildElements[index];
        return (Text)node;
    }

    /// <summary>
    /// Write the document to the temp directory. Useful for manual inspection during development.
    /// </summary>
    public static string WriteTempFile(string filename, byte[] file)
    {
        var path = Path.Combine(Path.GetTempPath(), filename);
        File.WriteAllBytes(path, file);
        return path;
    }

    public static XNamespace W => "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static XNamespace Wp => "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    public static XNamespace A => "http://schemas.openxmlformats.org/drawingml/2006/main";
    public static XNamespace Pic => "http://schemas.openxmlformats.org/drawingml/2006/picture";
    public static XNamespace R => "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
}
