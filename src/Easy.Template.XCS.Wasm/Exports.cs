using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;

[assembly: SupportedOSPlatform("browser")]

namespace Easy.Template.XCS.Wasm;

/// <summary>
/// JavaScript entry points for <see cref="TemplateHandler"/>.
/// Template data is passed as a JSON string (images as base64 strings).
/// </summary>
public static partial class Exports
{
    private static readonly TemplateHandler Handler = new();

    [JSExport]
    public static string Version() => TemplateHandler.Version;

    /// <summary>
    /// Process the template with the specified JSON data and return the resulting docx.
    /// </summary>
    [JSExport]
    public static byte[] Process(byte[] templateFile, string jsonData)
    {
        var data = JsonNode.Parse(jsonData);

        // The wasm runtime is single threaded and cannot block on pending tasks.
        // All the work is done over in-memory streams, so the task is expected
        // to complete synchronously.
        var task = Handler.ProcessAsync(templateFile, data);
        if (!task.IsCompleted)
            throw new InvalidOperationException("Template processing did not complete synchronously.");

        return task.GetAwaiter().GetResult();
    }

    public static void Main()
    {
        // Nothing to do, the module is driven through the exports above.
    }
}
