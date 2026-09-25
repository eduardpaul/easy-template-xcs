using Easy.Template.XCS.Errors;

namespace Easy.Template.XCS.Plugins.Image;

public static class ImageUtils
{
    public static string NameFromId(int imageId)
    {
        return $"Picture {imageId}";
    }

    public static long PixelsToEmu(double pixels)
    {
        // https://stackoverflow.com/questions/20194403/openxml-distance-size-units
        // https://docs.microsoft.com/en-us/windows/win32/vml/msdn-online-vml-units#other-units-of-measurement
        // https://en.wikipedia.org/wiki/Office_Open_XML_file_formats#DrawingML
        // http://www.java2s.com/Code/CSharp/2D-Graphics/ConvertpixelstoEMUEMUtopixels.htm
        return (long)Math.Round(pixels * 9525);
    }

    public static int TransparencyPercentToAlpha(double transparencyPercent)
    {
        if (transparencyPercent < 0 || transparencyPercent > 100)
            throw new TemplateDataException($"Transparency percent must be between 0 and 100, but was {transparencyPercent}.");

        return (int)Math.Round((100 - transparencyPercent) * 1000);
    }
}
