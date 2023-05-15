namespace test_easy_template_xcs;

public class UnitTest1
{
    [Fact]
    public async void Test1()
    {
        var templateHandler = new TemplateHandler();

        dynamic data = new System.Dynamic.ExpandoObject();
        data.author = "aaaaa";
        data.text = "bbbbb";
        data.posts = new object[] {new {author="aaa",text="bb"}, new {author="aaa",text="bb"}};

        using var inputStream = File.OpenRead("test.docx");
        var result = await templateHandler.Process(inputStream, data);
        File.WriteAllBytesAsync("test-result.docx", result.ToArray());
    }
}