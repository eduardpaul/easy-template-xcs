using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;

namespace Easy.Template.XCS;

public static class MimeType
{
    public const string Png = "image/png";
    public const string Jpeg = "image/jpeg";
    public const string Gif = "image/gif";
    public const string Bmp = "image/bmp";
    public const string Svg = "image/svg+xml";
}

public static class MimeTypeHelper
{
    public static string GetDefaultExtension(string mime)
    {
        return mime switch
        {
            MimeType.Png => "png",
            MimeType.Jpeg => "jpg",
            MimeType.Gif => "gif",
            MimeType.Bmp => "bmp",
            MimeType.Svg => "svg",
            _ => throw new UnsupportedFileTypeException(mime)
        };
    }

    public static string GetOfficeRelType(string mime)
    {
        return mime switch
        {
            MimeType.Png or MimeType.Jpeg or MimeType.Gif or MimeType.Bmp or MimeType.Svg => RelType.Image,
            _ => throw new UnsupportedFileTypeException(mime)
        };
    }
}
