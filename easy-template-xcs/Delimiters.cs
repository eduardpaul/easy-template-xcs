namespace EasyTemplateXCS;

public class Delimiters
{
    public string TagStart { get; set; } = "{";
    public string TagEnd { get; set; } = "}";
    public string ContainerTagOpen { get; set; } = "#";
    public string ContainerTagClose { get; set; } = "/";

    public Delimiters(Delimiters initial = null)
    {
        if (initial != null)
        {
            TagStart = initial.TagStart ?? "{";
            TagEnd = initial.TagEnd ?? "}";
            ContainerTagOpen = initial.ContainerTagOpen ?? "#";
            ContainerTagClose = initial.ContainerTagClose ?? "/";
        }

        EncodeAndValidate();

        if (ContainerTagOpen == ContainerTagClose)
        {
            throw new System.Exception($"{nameof(ContainerTagOpen)} can not be equal to {nameof(ContainerTagClose)}");
        }
    }

    private void EncodeAndValidate()
    {
        var keys = new List<string> { TagStart, TagEnd, ContainerTagOpen, ContainerTagClose };
        foreach (var value in keys)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new System.Exception($"{value} can not be empty.");
            }

            if (value != value.Trim())
            {
                throw new System.Exception($"{value} can not contain leading or trailing whitespace.");
            }
        }
    }
}
