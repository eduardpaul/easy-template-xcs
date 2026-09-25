namespace Easy.Template.XCS.Errors;

/// <summary>
/// Base class for all exceptions thrown by Easy.Template.XCS.
/// </summary>
public class EasyTemplateException : Exception
{
    public EasyTemplateException(string message) : base(message) { }

    public EasyTemplateException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// An unexpected internal state was reached. Should never happen, please report a bug if it does.
/// </summary>
public class InternalException : EasyTemplateException
{
    public InternalException(string message) : base($"Internal error: {message}") { }
}

public class InternalArgumentMissingException : InternalException
{
    public string ArgName { get; }

    public InternalArgumentMissingException(string argName) : base($"Argument '{argName}' is missing.")
    {
        ArgName = argName;
    }
}

/// <summary>
/// The input file is not a valid docx file or is corrupted.
/// </summary>
public class MalformedFileException : EasyTemplateException
{
    public MalformedFileException(string message) : base(message) { }

    public MalformedFileException(string message, Exception? innerException) : base(message, innerException) { }
}

public class MaxXmlDepthException : EasyTemplateException
{
    public int MaxDepth { get; }

    public MaxXmlDepthException(int maxDepth) : base($"XML maximum depth reached (max depth: {maxDepth}).")
    {
        MaxDepth = maxDepth;
    }
}

/// <summary>
/// The template contains invalid tag syntax (for instance, unbalanced delimiters or loop tags).
/// </summary>
public class TemplateSyntaxException : EasyTemplateException
{
    public TemplateSyntaxException(string message) : base(message) { }

    public TemplateSyntaxException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// The data passed to the template handler is invalid.
/// </summary>
public class TemplateDataException : EasyTemplateException
{
    public TemplateDataException(string message) : base(message) { }
}

public class MissingCloseDelimiterException : TemplateSyntaxException
{
    public string OpenDelimiterText { get; }

    public MissingCloseDelimiterException(string openDelimiterText) : base($"Close delimiter is missing from '{openDelimiterText}'.")
    {
        OpenDelimiterText = openDelimiterText;
    }
}

public class MissingStartDelimiterException : TemplateSyntaxException
{
    public string CloseDelimiterText { get; }

    public MissingStartDelimiterException(string closeDelimiterText) : base($"Open delimiter is missing from '{closeDelimiterText}'.")
    {
        CloseDelimiterText = closeDelimiterText;
    }
}

public class TagOptionsParseException : TemplateSyntaxException
{
    public string TagRawText { get; }

    public TagOptionsParseException(string tagRawText, Exception parseError)
        : base($"Failed to parse tag options of '{tagRawText}': {parseError.Message}.", parseError)
    {
        TagRawText = tagRawText;
    }
}

public class UnclosedTagException : TemplateSyntaxException
{
    public string TagName { get; }
    public string TagRawText { get; }

    public UnclosedTagException(string tagName, string tagRawText) : base($"Tag {tagRawText} is never closed.")
    {
        TagName = tagName;
        TagRawText = tagRawText;
    }
}

public class UnopenedTagException : TemplateSyntaxException
{
    public string TagName { get; }
    public string TagRawText { get; }

    public UnopenedTagException(string tagName, string tagRawText) : base($"Tag {tagRawText} is closed but was never opened.")
    {
        TagName = tagName;
        TagRawText = tagRawText;
    }
}

public class UnknownContentTypeException : TemplateDataException
{
    public string TagRawText { get; }
    public string ContentType { get; }
    public string Path { get; }

    public UnknownContentTypeException(string contentType, string tagRawText, string path)
        : base($"Content type '{contentType}' does not have a registered plugin to handle it.")
    {
        ContentType = contentType;
        TagRawText = tagRawText;
        Path = path;
    }
}

public class UnsupportedFileTypeException : EasyTemplateException
{
    public string FileType { get; }

    public UnsupportedFileTypeException(string fileType) : base($"Filetype \"{fileType}\" is not supported.")
    {
        FileType = fileType;
    }
}
