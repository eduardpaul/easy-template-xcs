namespace test_easy_template_xcs;

public class UnitTest1
{
    const string fixtureFiles = "../../../fixtures/files/";

    [Fact]
    public async void Test1()
    {
        var handler = new TemplateHandler();

        using (var inputStream = File.OpenRead(fixtureFiles + "simple.docx"))
        {
            var templateText = await handler.GetText<MainDocumentPart>(inputStream);
            Assert.Equal("{simple_prop}", templateText.Trim());
        }


        // replace tags
        var data = new
        {
            simple_prop = "hello world"
        };

        using (var inputStream = File.OpenRead(fixtureFiles + "simple.docx"))
        {
            var templateText = await handler.GetText<MainDocumentPart>(await handler.Process(inputStream, data));
            Assert.Equal("hello world", templateText.Trim());
        }
    }
}