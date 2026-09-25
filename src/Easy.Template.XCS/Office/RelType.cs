namespace Easy.Template.XCS.Office;

/// <summary>
/// The types of relationships that can be created in a docx file.
/// A non-comprehensive list.
/// </summary>
public static class RelType
{
    public const string Package = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/package";
    public const string MainDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    public const string Header = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";
    public const string Footer = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer";
    public const string Styles = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
    public const string Link = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";
    public const string Image = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
    public const string Chart = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart";
}
