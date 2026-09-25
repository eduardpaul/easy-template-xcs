namespace Easy.Template.XCS.Plugins.Image;

public class ImageContent : PluginContent
{
    public const string ContentTypeName = "image";

    public override string ContentType => ContentTypeName;

    /// <summary>
    /// The binary content of the image.
    /// </summary>
    public byte[]? Source { get; set; }

    /// <summary>
    /// The image mime type, see <see cref="MimeType"/>.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Width in pixels.
    ///
    /// When using an image placeholder, `Width` and `Height` properties are
    /// optional and if not set the image will keep its original size. Otherwise,
    /// they are required.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Height in pixels.
    ///
    /// When using an image placeholder, `Width` and `Height` properties are
    /// optional and if not set the image will keep its original size. Otherwise,
    /// they are required.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// Optional.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Optional. A value between 0 and 100. If this is not set, new images will
    /// be fully opaque and placeholder images will keep their original
    /// transparency.
    /// </summary>
    public double? TransparencyPercent { get; set; }
}
