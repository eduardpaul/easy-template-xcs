using Easy.Template.XCS.Errors;
using Easy.Template.XCS.Plugins.Image;
using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class ImageFixtureTests
{
    private static int CountMediaFiles(byte[] doc)
    {
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        return docx.GetAllParts().OfType<ImagePart>().Count();
    }

    private static List<string> ListMediaFiles(byte[] doc)
    {
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        return docx.GetAllParts().OfType<ImagePart>().Select(p => p.Uri.OriginalString).ToList();
    }

    //
    // basic
    //

    [Fact]
    public async Task Simple()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");
        var imageFile = ReadResource("panda1.jpg");
        var data = new
        {
            simple_prop = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = imageFile,
                Height = 325,
                Width = 600
            }
        };

        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        var drawing = Assert.Single(docXml!.Descendants(W + "drawing"));
        var inline = Assert.Single(drawing.Elements(Wp + "inline"));
        var extent = inline.Element(Wp + "extent")!;
        Assert.Equal((600 * 9525).ToString(), extent.Attribute("cx")?.Value);
        Assert.Equal((325 * 9525).ToString(), extent.Attribute("cy")?.Value);
        var docPr = inline.Element(Wp + "docPr")!;
        Assert.Equal("1", docPr.Attribute("id")?.Value);
        Assert.Equal("Picture 1", docPr.Attribute("name")?.Value);
        Assert.Null(docPr.Attribute("descr"));
        var blip = Assert.Single(drawing.Descendants(A + "blip"));
        var relId = blip.Attribute(R + "embed")?.Value;
        Assert.False(string.IsNullOrEmpty(relId));

        // the tag was removed
        Assert.Empty(docXml.Descendants(W + "t"));

        // the image was stored
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        var imagePart = Assert.IsType<ImagePart>(docx.MainDocumentPart!.GetPartById(relId!));
        Assert.Equal(MimeType.Jpeg, imagePart.ContentType);
        using var imageStream = imagePart.GetStream();
        Assert.Equal(imageFile.Length, imageStream.Length);
    }

    [Fact]
    public async Task Simple_DictionaryData()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");
        var imageFile = ReadResource("panda1.jpg");
        var data = new Dictionary<string, object?>
        {
            ["simple_prop"] = new Dictionary<string, object?>
            {
                ["_type"] = "image",
                ["format"] = "image/jpeg",
                ["source"] = imageFile,
                ["height"] = 325,
                ["width"] = 600
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        Assert.Single(docXml!.Descendants(W + "drawing"));
        Assert.Equal(1, CountMediaFiles(doc));
    }

    [Fact]
    public async Task AltText()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");
        var data = new
        {
            simple_prop = new ImageContent
            {
                Format = MimeType.Jpeg,
                AltText = "There is no spoon.",
                Source = ReadResource("panda1.jpg"),
                Height = 325,
                Width = 600
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        var docPr = Assert.Single(docXml!.Descendants(Wp + "docPr"));
        Assert.Equal("There is no spoon.", docPr.Attribute("descr")?.Value);
    }

    [Fact]
    public async Task Transparency()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");
        var data = new
        {
            simple_prop = new ImageContent
            {
                Format = MimeType.Jpeg,
                TransparencyPercent = 33,
                Source = ReadResource("panda1.jpg"),
                Height = 325,
                Width = 600
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        var alpha = Assert.Single(docXml!.Descendants(A + "alphaModFix"));
        Assert.Equal("67000", alpha.Attribute("amt")?.Value);
    }

    //
    // placeholder image
    //

    [Fact]
    public async Task Placeholder_Simple()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - placeholder.docx");
        var imageFile = ReadResource("panda1.jpg");

        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{My Tag 1}", templateText.Trim());
        var templateXml = await handler.GetXmlAsync(template);
        var docPrBefore = templateXml!.Descendants(Wp + "docPr").First();
        Assert.Equal("{My Tag 2}", docPrBefore.Attribute("descr")?.Value);
        var originalCx = templateXml.Descendants(Wp + "extent").First().Attribute("cx")?.Value;
        var originalEmbed = templateXml.Descendants(A + "blip").First().Attribute(R + "embed")?.Value;

        var data = new Dictionary<string, object?>
        {
            ["My Tag 2"] = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = imageFile,
                AltText = "There is no spoon."
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);

        // still a single image, with the new alt text and a new rel id
        var docPr = Assert.Single(docXml!.Descendants(Wp + "docPr"));
        Assert.Equal("There is no spoon.", docPr.Attribute("descr")?.Value);
        var embed = Assert.Single(docXml.Descendants(A + "blip")).Attribute(R + "embed")?.Value;
        Assert.NotEqual(originalEmbed, embed);

        // size is preserved
        Assert.Equal(originalCx, docXml.Descendants(Wp + "extent").First().Attribute("cx")?.Value);

        // the new image was added
        using var ms = new MemoryStream(doc);
        using var docx = WordprocessingDocument.Open(ms, false);
        var imagePart = Assert.IsType<ImagePart>(docx.MainDocumentPart!.GetPartById(embed!));
        Assert.Equal(MimeType.Jpeg, imagePart.ContentType);
    }

    [Fact]
    public async Task Placeholder_SizeOverride()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - placeholder.docx");
        var data = new Dictionary<string, object?>
        {
            ["My Tag 2"] = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = ReadResource("panda1.jpg"),
                AltText = "There is no spoon.",
                Width = 200,
                Height = 100
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        var extent = docXml!.Descendants(Wp + "extent").First();
        Assert.Equal((200 * 9525).ToString(), extent.Attribute("cx")?.Value);
        Assert.Equal((100 * 9525).ToString(), extent.Attribute("cy")?.Value);
        var ext = docXml.Descendants(A + "xfrm").First().Element(A + "ext")!;
        Assert.Equal((200 * 9525).ToString(), ext.Attribute("cx")?.Value);
        Assert.Equal((100 * 9525).ToString(), ext.Attribute("cy")?.Value);
    }

    [Fact]
    public async Task Placeholder_TransparencyOverride()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - placeholder.docx");
        var data = new Dictionary<string, object?>
        {
            ["My Tag 2"] = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = ReadResource("panda1.jpg"),
                AltText = "There is no spoon.",
                TransparencyPercent = 33
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        var alpha = Assert.Single(docXml!.Descendants(A + "alphaModFix"));
        Assert.Equal("67000", alpha.Attribute("amt")?.Value);
    }

    [Fact]
    public async Task Placeholder_MisplacedInChartAltText()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("chart - alt text.docx");
        var data = new
        {
            MyChart = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = ReadResource("panda1.jpg"),
                AltText = "There is no spoon."
            }
        };

        var error = await Assert.ThrowsAsync<TemplateSyntaxException>(() => handler.ProcessAsync(template, data));
        Assert.Contains("MyChart", error.Message);
    }

    //
    // multiple images
    //

    [Fact]
    public async Task AddToAnExistingImage()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - existing image.docx");
        var data = new
        {
            NewImage = new ImageContent
            {
                Format = MimeType.Jpeg,
                Source = ReadResource("panda1.jpg"),
                Height = 325,
                Width = 600
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);

        // two images with distinct ids
        var ids = docXml!.Descendants(Wp + "docPr").Select(p => p.Attribute("id")?.Value).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Equal(2, ids.Distinct().Count());
        Assert.Equal(2, CountMediaFiles(doc));
    }

    [Fact]
    public async Task TwoDifferentImages()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");
        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = ReadResource("panda1.jpg"), Height = 325, Width = 600 } },
                new { simple_prop = new ImageContent { Format = MimeType.Png, Source = ReadResource("panda2.png"), Height = 300, Width = 300 } }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);

        var embeds = docXml!.Descendants(A + "blip").Select(b => b.Attribute(R + "embed")?.Value).ToList();
        Assert.Equal(2, embeds.Count);
        Assert.Equal(2, embeds.Distinct().Count());

        var ids = docXml.Descendants(Wp + "docPr").Select(p => p.Attribute("id")?.Value).ToList();
        Assert.Equal(new[] { "1", "2" }, ids);

        var media = ListMediaFiles(doc);
        Assert.Equal(2, media.Count);
        Assert.Contains(media, m => m.EndsWith(".jpeg") || m.EndsWith(".jpg"));
        Assert.Contains(media, m => m.EndsWith(".png"));
    }

    [Fact]
    public async Task StartWithAnExistingImageAndRunTwice()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("image - existing image 2.docx");
        var imageFile1 = ReadResource("panda1.jpg");
        var imageFile2 = ReadResource("panda2.png");

        var data1 = new
        {
            Tag1 = new ImageContent { Format = MimeType.Png, Source = imageFile1, Height = 325, Width = 600 },
            Tag2 = "{NewTag1} {NewTag2}"
        };
        var data2 = new
        {
            NewTag1 = new ImageContent { Format = MimeType.Png, Source = imageFile2, Height = 300, Width = 300 },
            NewTag2 = "Done"
        };

        Assert.Equal(1, CountMediaFiles(template));

        // run the first time - should insert an image and two new tags
        var doc1 = await handler.ProcessAsync(template, data1);
        Assert.Equal(2, CountMediaFiles(doc1));
        Assert.Contains("{NewTag1} {NewTag2}", await handler.GetTextAsync(doc1));

        // run the second time - should insert another image
        var doc2 = await handler.ProcessAsync(doc1, data2);
        Assert.Equal(3, CountMediaFiles(doc2));
        Assert.Contains("Done", await handler.GetTextAsync(doc2));
        Assert.DoesNotContain("{NewTag1}", await handler.GetTextAsync(doc2));

        var docXml2 = await handler.GetXmlAsync(doc2);
        var ids = docXml2!.Descendants(Wp + "docPr").Select(p => p.Attribute("id")?.Value).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task InsertTheSameImageMultipleTimes_StoredOnlyOnce()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");
        var imageFile = ReadResource("panda1.jpg");
        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = imageFile, Height = 325, Width = 600 } },
                new { simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = imageFile, Height = 150, Width = 300 } },
                new { simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = imageFile, Height = 200, Width = 400 } }
            }
        };

        var doc = await handler.ProcessAsync(template, data);

        var docXml = await handler.GetXmlAsync(doc);
        Assert.Equal(3, docXml!.Descendants(W + "drawing").Count());
        Assert.Equal(1, CountMediaFiles(doc));
    }

    [Fact]
    public async Task UsingTheSameTemplateHandlerToAddTheSameImageToTwoDifferentFilesWorks()
    {
        var handler = new TemplateHandler();
        var template1 = ReadFixture("simple.docx");
        var template2 = ReadFixture("simple.docx");
        var imageFile = ReadResource("panda1.jpg");
        var data = new
        {
            simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = imageFile, Height = 325, Width = 600 }
        };

        var doc1 = await handler.ProcessAsync(template1, data);
        var doc2 = await handler.ProcessAsync(template2, data);

        Assert.Equal(1, CountMediaFiles(doc1));
        Assert.Equal(1, CountMediaFiles(doc2));
    }

    [Fact]
    public async Task RemovesTagIfSourceIsMissing()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("simple.docx");
        var data = new { simple_prop = new ImageContent { Format = MimeType.Jpeg } };

        var doc = await handler.ProcessAsync(template, data);
        var docXml = await handler.GetXmlAsync(doc);
        Assert.Empty(docXml!.Descendants(W + "drawing"));
        Assert.Equal("", (await handler.GetTextAsync(doc)).Trim());
    }

    //
    // performance
    //

    [Fact]
    public async Task ImageMarkupGenerationIsFastEnough()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");
        var imageFile = ReadResource("panda1.jpg");
        var data = new
        {
            loop_prop = Enumerable.Range(1, 1000).Select(_ => new
            {
                simple_prop = new ImageContent { Format = MimeType.Jpeg, Source = imageFile, Height = 50, Width = 100 }
            }).ToList()
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await handler.ProcessAsync(template, data);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Took {stopwatch.Elapsed}");
    }
}
