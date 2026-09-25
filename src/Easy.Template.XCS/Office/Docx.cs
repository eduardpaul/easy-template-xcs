using DocumentFormat.OpenXml.Packaging;
using Easy.Template.XCS.Errors;

namespace Easy.Template.XCS.Office;

/// <summary>
/// Helpers for working with a <see cref="WordprocessingDocument"/>.
/// </summary>
public static class Docx
{
    /// <summary>
    /// Load a docx file from a stream. The stream is copied into a new
    /// in-memory stream which is used as the backing store of the returned
    /// document. The original stream is left untouched.
    /// </summary>
    public static async Task<(WordprocessingDocument Document, MemoryStream Backing)> LoadAsync(Stream file, bool isEditable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var backing = new MemoryStream();
        await file.CopyToAsync(backing, cancellationToken).ConfigureAwait(false);
        backing.Position = 0;

        var document = Open(backing, isEditable);
        return (document, backing);
    }

    public static WordprocessingDocument Open(Stream backing, bool isEditable)
    {
        WordprocessingDocument document;
        try
        {
            document = WordprocessingDocument.Open(backing, isEditable);
        }
        catch (Exception e) when (e is OpenXmlPackageException or FileFormatException or InvalidDataException or IOException)
        {
            throw new MalformedFileException("Failed to load docx file.", e);
        }

        if (document.MainDocumentPart is null)
        {
            document.Dispose();
            throw new MalformedFileException("Cannot find main document part.");
        }

        return document;
    }

    /// <summary>
    /// Returns the parts that can contain template tags: the main document,
    /// headers and footers.
    /// </summary>
    public static IReadOnlyList<OpenXmlPart> GetContentParts(WordprocessingDocument document)
    {
        var mainPart = document.MainDocumentPart ?? throw new MalformedFileException("Cannot find main document part.");

        var parts = new List<OpenXmlPart> { mainPart };
        parts.AddRange(GetPartsByType(mainPart, RelType.Header));
        parts.AddRange(GetPartsByType(mainPart, RelType.Footer));
        return parts;
    }

    /// <summary>
    /// Get all parts related to the specified part by relationship type.
    /// </summary>
    public static IReadOnlyList<OpenXmlPart> GetPartsByType(OpenXmlPart part, string relType)
    {
        return part.Parts
            .Where(p => p.OpenXmlPart.RelationshipType == relType)
            .Select(p => p.OpenXmlPart)
            .ToList();
    }

    /// <summary>
    /// Get the parts of the document that match the specified relationship type.
    /// </summary>
    public static IReadOnlyList<OpenXmlPart> GetParts(WordprocessingDocument document, string relType)
    {
        var mainPart = document.MainDocumentPart ?? throw new MalformedFileException("Cannot find main document part.");

        if (relType == RelType.MainDocument)
            return new[] { mainPart };

        return GetPartsByType(mainPart, relType);
    }
}
