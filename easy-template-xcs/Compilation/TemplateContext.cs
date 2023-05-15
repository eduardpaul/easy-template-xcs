using DocumentFormat.OpenXml.Packaging;

namespace EasyTemplateXCS.Compilation
{
    public class TemplateContext
    {
        public OpenXmlPart CurrentPart;

        public OpenXmlPackage Docx { get; internal set; }
    }
}