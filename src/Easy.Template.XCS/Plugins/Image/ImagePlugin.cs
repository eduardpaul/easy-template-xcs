using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Xml;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace Easy.Template.XCS.Plugins.Image;

public class ImagePlugin : TemplatePlugin
{
    public override string ContentType => ImageContent.ContentTypeName;

    private sealed class ImagePluginContext
    {
        /// <summary>
        /// Last drawing object ID for each OOXML part.
        /// Key is the OOXML part uri.
        /// </summary>
        public Dictionary<string, int> LastDrawingObjectId { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Image parts already stored in the package, by content hash.
        /// </summary>
        public Dictionary<string, ImagePart>? MediaFilesByHash { get; set; }
    }

    public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        var content = data.GetScopeData<ImageContent>();
        if (content?.Source is null || content.Source.Length == 0)
        {
            OfficeMarkup.Modify.RemoveTag(tag);
            return Task.CompletedTask;
        }

        if (string.IsNullOrEmpty(content.Format))
            throw new TemplateDataException($"Image format is required for image tag \"{tag.RawText}\".");

        // validate the format
        MimeTypeHelper.GetOfficeRelType(content.Format);

        // add the image file into the package
        var imagePart = GetOrAddImagePart(context, content);
        var relId = GetOrAddRelationship(context.CurrentPart, imagePart);

        // generate a unique image ID
        var imageId = GetNextImageId(context);

        switch (tag)
        {
            // for text tags, create xml markup from scratch
            case TextNodeTag textNodeTag:
                {
                    var imageXml = CreateImage.Create(imageId, relId, content);
                    XmlNodes.InsertAfter(imageXml, textNodeTag.XmlTextNode);
                    break;
                }

            // for attribute tags, modify the existing markup
            case AttributeTag attributeTag:
                {
                    var drawingNode = XmlNodes.FindParent<Drawing>(attributeTag.XmlNode)
                        ?? throw new TemplateSyntaxException($"Cannot find placeholder image for tag \"{tag.RawText}\".");
                    UpdateImage.Update(tag, drawingNode, imageId, relId, content);
                    break;
                }
        }

        OfficeMarkup.Modify.RemoveTag(tag);
        return Task.CompletedTask;
    }

    private ImagePluginContext GetPluginContext(TemplateContext context)
    {
        if (!context.PluginContext.TryGetValue(ContentType, out var pluginContext))
        {
            pluginContext = new ImagePluginContext();
            context.PluginContext[ContentType] = pluginContext;
        }
        return (ImagePluginContext)pluginContext;
    }

    /// <summary>
    /// Store the image in the package. Identical images are stored only once.
    /// </summary>
    private ImagePart GetOrAddImagePart(TemplateContext context, ImageContent content)
    {
        var pluginContext = GetPluginContext(context);

        // index existing media files on first use
        if (pluginContext.MediaFilesByHash is null)
        {
            pluginContext.MediaFilesByHash = new Dictionary<string, ImagePart>(StringComparer.Ordinal);
            foreach (var existingPart in context.Docx.GetAllParts().OfType<ImagePart>())
            {
                using var stream = existingPart.GetStream(FileMode.Open, FileAccess.Read);
                var existingHash = Convert.ToHexString(SHA256.HashData(stream));
                pluginContext.MediaFilesByHash.TryAdd(existingHash, existingPart);
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(content.Source!));
        if (pluginContext.MediaFilesByHash.TryGetValue(hash, out var imagePart))
            return imagePart;

        imagePart = context.CurrentPart.AddNewPart<ImagePart>(content.Format!, GenerateRelId(context.CurrentPart));
        using (var stream = new MemoryStream(content.Source!, writable: false))
        {
            imagePart.FeedData(stream);
        }

        pluginContext.MediaFilesByHash[hash] = imagePart;
        return imagePart;
    }

    private static string GetOrAddRelationship(OpenXmlPart part, ImagePart imagePart)
    {
        foreach (var pair in part.Parts)
        {
            if (ReferenceEquals(pair.OpenXmlPart, imagePart))
                return pair.RelationshipId;
        }

        part.AddPart(imagePart, GenerateRelId(part));
        return part.GetIdOfPart(imagePart);
    }

    private static string GenerateRelId(OpenXmlPart part)
    {
        var existingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in part.Parts)
            existingIds.Add(pair.RelationshipId);
        foreach (var rel in part.ExternalRelationships)
            existingIds.Add(rel.Id);
        foreach (var rel in part.HyperlinkRelationships)
            existingIds.Add(rel.Id);
        foreach (var rel in part.DataPartReferenceRelationships)
            existingIds.Add(rel.Id);

        var i = existingIds.Count + 1;
        while (existingIds.Contains("rId" + i))
            i++;
        return "rId" + i;
    }

    private int GetNextImageId(TemplateContext context)
    {
        var pluginContext = GetPluginContext(context);
        var lastIdMap = pluginContext.LastDrawingObjectId;
        var lastIdKey = context.CurrentPart.Uri.OriginalString;

        // get next image ID if already initialized
        if (lastIdMap.TryGetValue(lastIdKey, out var lastId))
        {
            lastIdMap[lastIdKey] = lastId + 1;
            return lastId + 1;
        }

        // init next image ID - start counting from the current max
        var partRoot = context.CurrentPart.RootElement;
        var maxId = 0;
        if (partRoot != null)
        {
            foreach (var docPr in partRoot.Descendants<DW.DocProperties>())
            {
                if (docPr.Id?.HasValue == true && docPr.Id.Value > maxId)
                    maxId = (int)docPr.Id.Value;
            }
        }

        lastIdMap[lastIdKey] = maxId + 1;
        return maxId + 1;
    }
}
