using System.Text.RegularExpressions;

namespace Easy.Template.XCS.Utils;

public static class TextUtils
{
    // Copied from: https://gist.github.com/thanpolas/244d9a13151caf5a12e42208b6111aa6
    // And see: https://unicode-table.com/en/sets/quotation-marks/
    private static readonly Regex NonStandardDoubleQuotesRegex = new(
        "[“”«»„‟❝❞〝〞〟＂]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizeDoubleQuotes(string text)
    {
        return NonStandardDoubleQuotesRegex.Replace(text, "\"");
    }
}
