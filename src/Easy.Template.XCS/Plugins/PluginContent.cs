using System.Text.Json.Serialization;

namespace Easy.Template.XCS.Plugins;

/// <summary>
/// Base class for template data values that explicitly specify the plugin
/// that should handle them (for instance images and links).
///
/// Plugin content can also be specified as a dictionary (or json object)
/// with a "_type" key.
/// </summary>
public abstract class PluginContent
{
    /// <summary>
    /// The content type of this content (i.e. the plugin that handles it).
    /// </summary>
    [JsonPropertyName("_type")]
    public abstract string ContentType { get; }
}
