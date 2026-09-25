namespace Easy.Template.XCS;

/// <summary>
/// The delimiters used to identify tags in the template.
/// </summary>
public class Delimiters
{
    public string TagStart { get; set; } = "{";
    public string TagEnd { get; set; } = "}";
    public string ContainerTagOpen { get; set; } = "#";
    public string ContainerTagClose { get; set; } = "/";
    public string TagOptionsStart { get; set; } = "[";
    public string TagOptionsEnd { get; set; } = "]";

    public Delimiters()
    {
    }

    /// <summary>
    /// Create a validated copy of the specified delimiters.
    /// </summary>
    public Delimiters(Delimiters? initial)
    {
        if (initial != null)
        {
            TagStart = initial.TagStart;
            TagEnd = initial.TagEnd;
            ContainerTagOpen = initial.ContainerTagOpen;
            ContainerTagClose = initial.ContainerTagClose;
            TagOptionsStart = initial.TagOptionsStart;
            TagOptionsEnd = initial.TagOptionsEnd;
        }

        Validate();
    }

    public void Validate()
    {
        var keys = new (string Name, string? Value)[]
        {
            (nameof(TagStart), TagStart),
            (nameof(TagEnd), TagEnd),
            (nameof(ContainerTagOpen), ContainerTagOpen),
            (nameof(ContainerTagClose), ContainerTagClose),
            (nameof(TagOptionsStart), TagOptionsStart),
            (nameof(TagOptionsEnd), TagOptionsEnd),
        };

        foreach (var (name, value) in keys)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{name} can not be empty.");

            if (value != value.Trim())
                throw new ArgumentException($"{name} can not contain leading or trailing whitespace.");
        }

        if (ContainerTagOpen == ContainerTagClose)
            throw new ArgumentException($"{nameof(ContainerTagOpen)} can not be equal to {nameof(ContainerTagClose)}");
    }
}
