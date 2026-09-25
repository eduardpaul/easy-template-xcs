# Easy.Template.XCS

Generate docx documents from templates in .NET.

A port of [easy-template-x](https://github.com/alonrbar/easy-template-x) (the JavaScript library by Alon Bar) to modern .NET, built on top of the [Open XML SDK](https://github.com/dotnet/Open-XML-SDK).

- Targets `net8.0` and `net10.0`.
- Same template syntax as `easy-template-x`: `{tags}`, `{#loops}{/loops}`, conditions, images, links, raw xml.
- Template data can be anonymous objects, POCOs, dictionaries or `System.Text.Json` nodes.

## Quick start

```csharp
using Easy.Template.XCS;

// 1. read template file
var templateFile = await File.ReadAllBytesAsync("myTemplate.docx");

// 2. process the template
var data = new
{
    posts = new[]
    {
        new { author = "Alon Bar", text = "Very important\ntext here!" },
        new { author = "Alon Bar", text = "Forgot to mention that..." }
    }
};

var handler = new TemplateHandler();
var doc = await handler.ProcessAsync(templateFile, data);

// 3. save output
await File.WriteAllBytesAsync("myTemplate - output.docx", doc);
```

Input template:

```
{#posts}
{author}
{text}
{/posts}
```

Output document:

```
Alon Bar
Very important
text here!
Alon Bar
Forgot to mention that...
```

`ProcessAsync` has overloads for `byte[]` and `Stream` (the input stream is not modified nor disposed).

## Template data

The `data` argument can be any of the following, mixed freely:

| Data type | Example |
| --- | --- |
| Anonymous objects, records and POCOs | `new { name = "Alice", items = new[] { ... } }` |
| Dictionaries (`IDictionary<string, T>`, `ExpandoObject`, ...) | `new Dictionary<string, object?> { ["my tag"] = "value" }` |
| `System.Text.Json` (`JsonNode`, `JsonElement`) | `JsonNode.Parse(json)` |
| Lists and arrays (for loops) | `List<T>`, `T[]`, `JsonArray` |
| Primitives | `string`, numbers, `bool` |
| Plugin content | `new ImageContent { ... }` or a dictionary with a `"_type"` key |

For objects, tag names are matched to property (or field) names case-sensitively first, then case-insensitively. Dictionary keys are matched exactly. Use dictionaries for tag names that are not valid C# identifiers (for instance `{Student Name}`).

Values are converted to text using the invariant culture (`true`/`false` for booleans, `null` becomes an empty string).

## Plugins

`Easy.Template.XCS` uses a plugin model to support its various template manipulation capabilities. There are some built-in plugins and you can also write your own custom plugins if required.

- [Text plugin](#text-plugin) - For simple text replacement.
- [Loop plugin](#loop-plugin) - For iterating text, table rows, table columns and list rows and for simple conditions.
- [Image plugin](#image-plugin) - For embedding images.
- [Link plugin](#link-plugin) - For hyperlinks creation.
- [Raw xml plugin](#raw-xml-plugin) - For custom xml insertion.

### Text plugin

The most basic plugin. Replaces a single tag with custom text. Preserves the original text style. Newlines (`\n`) are converted to line breaks.

```csharp
var data = new Dictionary<string, object?>
{
    ["First Tag"] = "Quis et ducimus voluptatum\nipsam id.",
    ["Second Tag"] = "Dolorem sit voluptas magni dolorem molestias."
};
```

Text tags can also be placed in the *alt text* of images (see [image placeholders](#image-placeholder-replacement)).

### Loop plugin

Iterates text, table rows, table columns and lists.
Requires an opening tag that starts with `#` and a closing tag that starts with `/` ([configurable](#custom-tag-delimiters)).

**Note**: The closing tag does not need to have the same name as the opening tag, or a name at all. This will work `{#loop}{/loop}`, but also this `{#loop}{/}` and even this `{#loop}{/something else}`.

Input template (a table with one row):

| Brand | Price |
| --- | --- |
| `{#Beers}{Brand}` | `{Price}{/Beers}` |

Input data:

```csharp
var data = new
{
    Beers = new[]
    {
        new { Brand = "Carlsberg", Price = 1 },
        new { Brand = "Leaf Blonde", Price = 2 },
        new { Brand = "Weihenstephan", Price = 1.5 }
    }
};
```

#### Conditions

You can render content conditionally depending on a boolean value using the same syntax used for loops:

```
{#visible}This is rendered only if "visible" is truthy.{/visible}
```

Truthiness follows JavaScript rules: `null`, `false`, `0`, `""` and `NaN` are false, everything else (including empty objects) is true. Lists are always looped (an empty list renders nothing).

#### Nested conditions

Nested conditions are also supported, so you can nest other tags including loop tags and even other conditions in them. When doing so remember to format your data accordingly: even though `name` and `members` are nested in the template under the `show` condition their values are adjacent to it in the input data.

Input template:

```
{#teams}{#show}{name}: {#members}{name}, {/members}{/show}{/teams}
```

Input data:

```csharp
var data = new
{
    teams = new[]
    {
        new { show = true, name = "A-Team", members = new[] { new { name = "Hannibal" }, new { name = "Face" } } },
        new { show = false, name = "B-Team", members = new[] { new { name = "Alice" }, new { name = "Bob" } } }
    }
};
```

#### Controlling loop behavior

The loop plugin uses some heuristics to determine the right behavior in each case (i.e. when to loop over rows, columns, paragraphs, etc.). You can control this behavior explicitly through the `loopOver` option.

The default heuristics are as follows:

1. If both loop tags are inside the same table cell - the plugin assumes you want to repeat the **cell content**, not the row or column.
2. If both loop tags are in the same column - the plugin will repeat the **column**.
3. If both loop tags are inside a table - the plugin will repeat the relevant table **rows**.
4. If both loop tags are in a list - the plugin will repeat the relevant **list items**.
5. Otherwise - the plugin will use a naive approach for repeating the **content** in between the loop tags.

To use a different behavior specify the `loopOver` tag option. Supported values are: `row`, `column`, `paragraph` and `content`. This option controls conditions too.

```
{#students [loopOver: "row"]}{name}{/students}
{#students [loopOver: "paragraph"]}{name}{/students}
```

Tag options use a lenient JSON-like syntax (unquoted keys, single or double quotes) and the delimiters are [configurable](#custom-tag-delimiters).

### Image plugin

Embed images into the document. The image plugin supports two modes:

1. Text tag replacement - insert inline images by placing tags in the document body.
2. Image placeholder replacement - replace existing placeholder images while preserving their size, position and styling.

#### Image placeholder replacement

You can use any image you want as the placeholder. To let `Easy.Template.XCS` know you want to replace the placeholder image, insert a tag in its *alt text* (right click the image in Word, "View Alt Text...").

#### Data and output

The data is the same for both modes:

```csharp
using Easy.Template.XCS.Plugins.Image;

var data = new Dictionary<string, object?>
{
    ["Kung Fu Hero"] = new ImageContent
    {
        Source = await File.ReadAllBytesAsync("hero.png"),
        Format = MimeType.Png,
        Width = 200,                    // Required for text tags, optional for placeholders (pixels)
        Height = 200,                   // Required for text tags, optional for placeholders (pixels)
        AltText = "Kung Fu Hero",       // Optional
        TransparencyPercent = 80        // Optional
    }
};
```

Or, using a dictionary:

```csharp
["Kung Fu Hero"] = new Dictionary<string, object?>
{
    ["_type"] = "image",
    ["source"] = imageBytes,
    ["format"] = "image/png",
    ["width"] = 200,
    ["height"] = 200
}
```

Supported formats: `image/png`, `image/jpeg`, `image/gif`, `image/bmp`, `image/svg+xml` (see `MimeType`). Identical images are stored in the document only once.

### Link plugin

Inserts hyperlinks into the document. Like text tags, link tags also preserve their original style.

```csharp
using Easy.Template.XCS.Plugins.Link;

var data = new
{
    easy = new LinkContent
    {
        Text = "super easy",     // Optional - if not specified the Target property will be used
        Target = "https://github.com/alonrbar/easy-template-x",
        Tooltip = "Click me"     // Optional
    }
};
```

### Raw xml plugin

Add custom xml into the document to be interpreted by Word.

**Tip**: You can add page breaks using this plugin and the following xml markup: `<w:br w:type="page"/>`

```csharp
using Easy.Template.XCS.Plugins.RawXml;

var data = new Dictionary<string, object?>
{
    ["Dont worry be happy"] = new RawXmlContent
    {
        Xml = "<w:sym w:font=\"Wingdings\" w:char=\"F04A\"/>",  // string or IEnumerable<string>
        ReplaceParagraph = false   // Optional - should the plugin replace an entire paragraph or just the tag itself
    }
};
```

Common namespace prefixes (`w`, `r`, `wp`, `a`, `pic`, `mc`, `w14`, ...) are pre-declared, so snippets can use them without declaring namespaces.

### Writing your own plugins

To write a plugin inherit from the `TemplatePlugin` class and add it to the `Plugins` list of the handler options. The base class provides two methods you can override (`SimpleTagReplacementsAsync` and `ContainerTagReplacementsAsync`), and the `OfficeMarkup` and `XmlNodes` static classes provide utilities that make it easier to do the actual xml modification.

_To better understand the internal structure of Word documents check out [this excellent source](http://officeopenxml.com/WPcontentOverview.php)._

Example plugin implementation (a simplified version of the raw xml plugin):

```csharp
using Easy.Template.XCS.Compilation;
using Easy.Template.XCS.Office;
using Easy.Template.XCS.Plugins;
using Easy.Template.XCS.Plugins.RawXml;
using Easy.Template.XCS.Xml;

public class MyRawXmlPlugin : TemplatePlugin
{
    // Declare the unique "content type" this plugin handles
    public override string ContentType => "rawXml";

    public override Task SimpleTagReplacementsAsync(Tag tag, ScopeData data, TemplateContext context)
    {
        // Get the value to use from the input data (dictionaries and json
        // objects are mapped to the content class automatically).
        var value = data.GetScopeData<RawXmlContent>();

        // Tags placed in the document text are TextNodeTag instances and
        // reference the actual <w:t> node.
        if (tag is TextNodeTag textTag && value?.GetXmlString() is string xml)
        {
            foreach (var newNode in RawXmlPlugin.ParseFragment(xml))
                XmlNodes.InsertBefore(newNode, textTag.XmlTextNode);
        }

        // Remove the placeholder tag.
        OfficeMarkup.Modify.RemoveTag(tag);
        return Task.CompletedTask;
    }
}

// Content classes derive from PluginContent:
public class RawXmlContent : PluginContent
{
    public override string ContentType => "rawXml";
    public string? Xml { get; set; }
}
```

## Listing tags

You can get the list of tags in a template (main document, headers and footers) by calling the `ParseTagsAsync` method:

```csharp
var handler = new TemplateHandler();
var tags = await handler.ParseTagsAsync(templateFile);
foreach (var tag in tags)
    Console.WriteLine($"{tag.Name} ({tag.Disposition}): {tag.RawText}");
```

## Reading documents

`GetTextAsync` and `GetXmlAsync` return the text content / xml root of a document part. They are mostly useful for testing:

```csharp
var text = await handler.GetTextAsync(doc);                     // main document
var headerText = await handler.GetTextAsync(doc, RelType.Header);
XElement? xml = await handler.GetXmlAsync(doc);
```

## Scope resolution

`Easy.Template.XCS` supports tag data scoping. That is, you can reference "shallow" data from within deeper in the hierarchy, similarly to how you can reference an outer scope variable from within a function. You can leverage this property to declare "top level" data (your logo and company name or some useful xml snippets like page breaks, etc.) to be used anywhere in the document.

Input template (notice that we are using the "Company" tag inside the "Employees" loop):

```
{#Employees}{Given name} {Surname}, {Company}{/Employees}
```

Input data (notice that the "Company" data is declared outside the "Employees" loop):

```csharp
var data = new Dictionary<string, object?>
{
    ["Company"] = "Contoso Ltd.",
    ["Employees"] = new[]
    {
        new Dictionary<string, object?> { ["Surname"] = "Gates", ["Given name"] = "William" },
        new Dictionary<string, object?> { ["Surname"] = "Nadella", ["Given name"] = "Satya" }
    }
};
```

## Extensions

While most document manipulation can be achieved using plugins, there are some cases where a more powerful tool is required. In order to extend the document manipulation process you can specify extensions that will be run before and/or after the standard template processing of each part (main document, headers and footers).

To write an extension inherit from the `TemplateExtension` class. By default no extension is loaded. Extensions and the order they run in are specified via the `TemplateHandlerOptions`:

```csharp
var handler = new TemplateHandler(new TemplateHandlerOptions
{
    Extensions = new ExtensionOptions
    {
        AfterCompilation = { new MyExtension() }
    }
});
```

## Template handler options

You can configure the template handler behavior by passing an options object to its constructor. Below is the list of options along with their default values:

```csharp
var handler = new TemplateHandler(new TemplateHandlerOptions
{
    Plugins = DefaultPlugins.Create(),   // IList<TemplatePlugin>

    SkipEmptyTags = false,               // leave tags with empty data untouched

    DefaultContentType = "text",         // plugin used for tags whose data has no explicit content type

    ContainerContentType = "loop",       // plugin used for container (open/close) tags

    Delimiters = new Delimiters
    {
        TagStart = "{",
        TagEnd = "}",
        ContainerTagOpen = "#",
        ContainerTagClose = "/",
        TagOptionsStart = "[",
        TagOptionsEnd = "]"
    },

    MaxXmlDepth = 20,

    Extensions = new ExtensionOptions(), // BeforeCompilation / AfterCompilation lists

    ScopeDataResolver = null             // ScopeDataResolver delegate
});
```

### Custom tag delimiters

To use custom tag delimiters and container marks (used for loops and conditions) specify the `Delimiters` option of the template handler.

For instance, to change from `{#open loop}` and `{/close loop}` to `{{>>open loop}}` and `{{<<close loop}}` do the following:

```csharp
var handler = new TemplateHandler(new TemplateHandlerOptions
{
    Delimiters = new Delimiters
    {
        TagStart = "{{",
        TagEnd = "}}",
        ContainerTagOpen = ">>",
        ContainerTagClose = "<<"
    }
});
```

### Advanced syntax and custom resolvers

Custom scope data resolvers give you a way to hook into `Easy.Template.XCS` in order to change how it interprets the tag syntax. The resolver receives the current path (the tag chain and loop indexes) and the root data object and returns the value of the current tag:

```csharp
var handler = new TemplateHandler(new TemplateHandlerOptions
{
    ScopeDataResolver = args =>
    {
        // args.Path    - the tags and loop indexes leading to the current tag
        // args.StrPath - the same path as strings, e.g. ["Employees", "0", "Surname"]
        // args.Data    - the root data object
        return MyExpressionEvaluator.Evaluate(args.StrPath[^1], args.Data);
    }
});
```

The default resolver is available as `ScopeData.DefaultResolver`.

## Errors

All errors thrown by the library derive from `EasyTemplateException` (namespace `Easy.Template.XCS.Errors`):

- `TemplateSyntaxException` - invalid template syntax (`MissingCloseDelimiterException`, `MissingStartDelimiterException`, `UnclosedTagException`, `UnopenedTagException`, `TagOptionsParseException`).
- `TemplateDataException` - invalid data (`UnknownContentTypeException`).
- `MalformedFileException` - the input is not a valid docx file.
- `MaxXmlDepthException` - the document is nested deeper than `MaxXmlDepth`.

## Differences from easy-template-x

- The chart plugin (and the underlying xlsx support) is not ported.
- Tags are searched in WordprocessingML text (`w:t`) only. DrawingML text (`a:t`, used by charts and SmartArt) is not processed.
- Loop content is cloned with unique bookmark ids (the JavaScript version duplicates them).
- Public API follows .NET conventions (`PascalCase`, `Async` suffix, `ContentType` instead of `_type` on content classes). Dictionaries and json objects still use the `_type` key.

## Note - Internal API

In addition to what's described here in the readme file the library exposes many more types. While you are free to use them as you see fit please note that anything not documented in the readme file is considered an internal implementation detail and may break between minor versions, use at your own risk.

## Development

```sh
cd src
dotnet build
dotnet test
```

The test suite uses the original `easy-template-x` fixture documents (in `src/Easy.Template.XCS.Test/Fixtures`) and validates the generated documents against the Open XML schema.

## License

[MIT](LICENSE).

This project is a derivative work of [easy-template-x](https://github.com/alonrbar/easy-template-x) by Alon Bar, which is also MIT licensed. Both copyright notices are reproduced in [LICENSE](LICENSE), along with a summary of which parts derive from the original — including the test fixture documents under `src/Easy.Template.XCS.Test/Fixtures/`, which are copied verbatim from the easy-template-x test suite.
