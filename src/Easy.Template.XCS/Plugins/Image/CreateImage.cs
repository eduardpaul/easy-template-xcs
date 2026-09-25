using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Errors;
using A = DocumentFormat.OpenXml.Drawing;
using A14 = DocumentFormat.OpenXml.Office2010.Drawing;
using ADEC = DocumentFormat.OpenXml.Office2019.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Easy.Template.XCS.Plugins.Image;

internal static class CreateImage
{
    private const string PictureGraphicDataUri = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private const string UseLocalDpiExtensionUri = "{28A0092B-C50C-407E-A947-70E740481C1C}";
    private const string DecorativeExtensionUri = "{C183D7F6-B498-43B3-948B-1728B52AA6E4}";

    /// <summary>
    /// Create an inline image markup (a <c>w:drawing</c> element).
    /// See: http://officeopenxml.com/drwPicInline.php
    /// </summary>
    public static Drawing Create(int imageId, string relId, ImageContent content)
    {
        if (content.Width is null || content.Height is null)
            throw new TemplateDataException("Image width and height are required when the image is not replacing a placeholder image.");

        var name = ImageUtils.NameFromId(imageId);
        var cx = ImageUtils.PixelsToEmu(content.Width.Value);
        var cy = ImageUtils.PixelsToEmu(content.Height.Value);

        var inline = new DW.Inline(
            new DW.Extent { Cx = cx, Cy = cy },
            new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
            DocProperties(imageId, name, content),
            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(new A.GraphicData(PictureMarkup(imageId, relId, name, content, cx, cy)) { Uri = PictureGraphicDataUri }))
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

        return new Drawing(inline);
    }

    private static DW.DocProperties DocProperties(int imageId, string name, ImageContent content)
    {
        if (!string.IsNullOrEmpty(content.AltText))
            return new DW.DocProperties { Id = (uint)imageId, Name = name, Description = content.AltText };

        // no alt text - mark the image as decorative
        return new DW.DocProperties(
            new A.NonVisualDrawingPropertiesExtensionList(
                new A.NonVisualDrawingPropertiesExtension(new ADEC.Decorative { Val = true }) { Uri = DecorativeExtensionUri }))
        {
            Id = (uint)imageId,
            Name = name
        };
    }

    private static PIC.Picture PictureMarkup(int imageId, string relId, string name, ImageContent content, long cx, long cy)
    {
        // http://officeopenxml.com/drwPic.php
        // Legend:
        // nvPicPr - non-visual picture properties - id, name, etc.
        // blipFill - binary large image (or) picture fill - image size, image fill, etc.
        // spPr - shape properties - frame size, frame fill, etc.

        var blip = new A.Blip { Embed = relId };
        if (content.TransparencyPercent != null)
            blip.AppendChild(new A.AlphaModulationFixed { Amount = ImageUtils.TransparencyPercentToAlpha(content.TransparencyPercent.Value) });
        blip.AppendChild(new A.BlipExtensionList(
            new A.BlipExtension(new A14.UseLocalDpi { Val = false }) { Uri = UseLocalDpiExtensionUri }));

        return new PIC.Picture(
            new PIC.NonVisualPictureProperties(
                new PIC.NonVisualDrawingProperties { Id = (uint)imageId, Name = name },
                new PIC.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true, NoChangeArrowheads = true })),
            new PIC.BlipFill(
                blip,
                new A.SourceRectangle(),
                new A.Stretch(new A.FillRectangle())),
            new PIC.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = cx, Cy = cy }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle },
                new A.NoFill(),
                new A.Outline(new A.NoFill()))
            {
                BlackWhiteMode = A.BlackWhiteModeValues.Auto
            });
    }
}
