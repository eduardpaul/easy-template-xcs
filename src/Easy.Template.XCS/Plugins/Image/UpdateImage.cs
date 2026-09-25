using DocumentFormat.OpenXml;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Easy.Template.XCS.Plugins.Image;

/// <summary>
/// Updates an existing (placeholder) image markup.
/// </summary>
internal static class UpdateImage
{
    public static void Update(Tag tag, OpenXmlElement drawingContainerNode, int imageId, string relId, ImageContent content)
    {
        var inlineNode = drawingContainerNode.ChildElements.OfType<DW.Inline>().FirstOrDefault();
        var floatingNode = drawingContainerNode.ChildElements.OfType<DW.Anchor>().FirstOrDefault();
        OpenXmlElement drawingNode = (OpenXmlElement?)inlineNode ?? floatingNode
            ?? throw new MalformedFileException("Invalid drawing container node. Expected inline or floating anchor node.");

        var pictureNode = drawingNode.ChildElements.OfType<A.Graphic>().FirstOrDefault()
            ?.ChildElements.OfType<A.GraphicData>().FirstOrDefault()
            ?.ChildElements.OfType<PIC.Picture>().FirstOrDefault();
        if (pictureNode is null)
        {
            throw new TemplateSyntaxException(
                $"Invalid template syntax for image tag \"{tag.RawText}\". " +
                "Please make sure the tag is placed in the alt text of an image placeholder.");
        }

        // set rel ID
        SetRelId(pictureNode, relId);

        // update non-visual properties
        UpdateNonVisualProps(drawingNode, pictureNode, imageId, content);

        // update size
        UpdateSize(drawingNode, pictureNode, content);

        // update transparency
        UpdateTransparency(pictureNode, content);
    }

    private static A.Blip GetBlip(PIC.Picture pictureNode)
    {
        return pictureNode.BlipFill?.Blip
            ?? throw new MalformedFileException("Cannot find blip node.");
    }

    private static void SetRelId(PIC.Picture pictureNode, string relId)
    {
        GetBlip(pictureNode).Embed = relId;
    }

    private static void UpdateNonVisualProps(OpenXmlElement drawingNode, PIC.Picture pictureNode, int imageId, ImageContent content)
    {
        var docPrNode = drawingNode.ChildElements.OfType<DW.DocProperties>().FirstOrDefault()
            ?? throw new MalformedFileException("Cannot find doc properties node.");

        var nvPicPrNode = pictureNode.NonVisualPictureProperties?.NonVisualDrawingProperties
            ?? throw new MalformedFileException("Cannot find non-visual picture properties node.");

        docPrNode.Id = (uint)imageId;
        nvPicPrNode.Id = (uint)imageId;

        var imageName = ImageUtils.NameFromId(imageId);
        docPrNode.Name = imageName;
        nvPicPrNode.Name = imageName;

        if (!string.IsNullOrEmpty(content.AltText))
        {
            docPrNode.Description = content.AltText;
            nvPicPrNode.Description = content.AltText;
        }
    }

    private static void UpdateSize(OpenXmlElement drawingNode, PIC.Picture pictureNode, ImageContent content)
    {
        if (content.Width is null && content.Height is null)
            return;

        var drawingExtentNode = drawingNode.ChildElements.OfType<DW.Extent>().FirstOrDefault()
            ?? throw new MalformedFileException("Cannot find drawing extent node.");

        var pictureExtentNode = pictureNode.ShapeProperties?.Transform2D?.Extents
            ?? throw new MalformedFileException("Cannot find picture extent node.");

        if (content.Width != null)
        {
            var widthEmu = ImageUtils.PixelsToEmu(content.Width.Value);
            drawingExtentNode.Cx = widthEmu;
            pictureExtentNode.Cx = widthEmu;
        }

        if (content.Height != null)
        {
            var heightEmu = ImageUtils.PixelsToEmu(content.Height.Value);
            drawingExtentNode.Cy = heightEmu;
            pictureExtentNode.Cy = heightEmu;
        }
    }

    private static void UpdateTransparency(PIC.Picture pictureNode, ImageContent content)
    {
        if (content.TransparencyPercent is null)
            return;

        var blipNode = GetBlip(pictureNode);
        var alphaNode = blipNode.ChildElements.OfType<A.AlphaModulationFixed>().FirstOrDefault();

        // if the alpha node is not present, create it
        if (alphaNode is null)
        {
            alphaNode = new A.AlphaModulationFixed();
            blipNode.InsertAt(alphaNode, 0);
        }

        // set the alpha value
        alphaNode.Amount = ImageUtils.TransparencyPercentToAlpha(content.TransparencyPercent.Value);
    }
}
