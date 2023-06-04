using DocumentFormat.OpenXml.Packaging;

namespace Easy.Template.XCS.Compilation
{
    public class TemplateContext
    {
        public OpenXmlPart CurrentPart;

        public OpenXmlPackage Docx { get; internal set; }
    }
}