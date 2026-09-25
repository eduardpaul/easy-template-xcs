using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Easy.Template.XCS.Compilation;
using Tag = Easy.Template.XCS.Compilation.Tag;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;
using WText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Easy.Template.XCS.Plugins.Link;

public class LinkPlugin : TemplatePlugin
{
    public override string ContentType => LinkContent.ContentTypeName;

    public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        if (tag is not TextNodeTag textNodeTag)
            throw new TemplateSyntaxException($"Link tag \"{tag.RawText}\" must be placed in a text node but was placed in {tag.Placement}");

        var content = data.GetScopeData<LinkContent>();
        if (content is null || string.IsNullOrEmpty(content.Target))
        {
            OfficeMarkup.Modify.RemoveTag(textNodeTag);
            return Task.CompletedTask;
        }

        // add rel
        var relId = context.CurrentPart.AddHyperlinkRelationship(new Uri(content.Target, UriKind.RelativeOrAbsolute), true).Id;

        // generate markup
        var textNode = textNodeTag.XmlTextNode;
        var runNode = OfficeMarkup.Query.ContainingRunNode(textNode)
            ?? throw new TemplateSyntaxException($"Tag {tag.RawText} is not inside a run.");
        var linkMarkup = GenerateMarkup(content, relId, runNode);

        // add to document
        InsertHyperlinkNode(linkMarkup, runNode, textNode);

        return Task.CompletedTask;
    }

    private static Hyperlink GenerateMarkup(LinkContent content, string relId, Run runNode)
    {
        // http://officeopenxml.com/WPhyperlink.php

        // copy props from original run node (preserve style)
        var runProps = runNode.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties();
        runProps.RunStyle = new RunStyle { Val = "Hyperlink" };

        var run = new Run(runProps, new WText(content.Text ?? content.Target ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });

        var hyperlink = new Hyperlink(run)
        {
            Id = relId,
            History = true
        };

        if (!string.IsNullOrEmpty(content.Tooltip))
            hyperlink.Tooltip = content.Tooltip;

        return hyperlink;
    }

    private static void InsertHyperlinkNode(Hyperlink linkMarkup, Run tagRunNode, WText tagTextNode)
    {
        // Links are inserted at the 'run' level.
        // Therefor we isolate the link tag to it's own run (it is already
        // isolated to it's own text node), insert the link markup and remove
        // the run.
        var textNodesInRun = tagRunNode.ChildElements.OfType<WText>().Count();
        if (textNodesInRun > 1)
        {
            var (runBeforeTag, runAfterTag) = XmlNodes.SplitByChild(tagRunNode, tagTextNode, true);
            XmlNodes.InsertAfter(linkMarkup, runBeforeTag);

            if (OfficeMarkup.Query.IsEmptyRun(runBeforeTag))
                XmlNodes.Remove(runBeforeTag);
            if (OfficeMarkup.Query.IsEmptyRun(runAfterTag))
                XmlNodes.Remove(runAfterTag);
        }
        else
        {
            // already isolated
            XmlNodes.InsertAfter(linkMarkup, tagRunNode);
            XmlNodes.Remove(tagRunNode);
        }
    }
}
