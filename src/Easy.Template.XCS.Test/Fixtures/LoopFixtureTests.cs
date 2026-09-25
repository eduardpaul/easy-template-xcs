using static Easy.Template.XCS.Test.TestUtils;

namespace Easy.Template.XCS.Test.Fixtures;

public class LoopFixtureTests
{
    //
    // base
    //

    [Fact]
    public async Task SimpleParagraphLoops()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop_prop}{simple_prop}!{/loop_prop}", templateText.Trim());

        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop = "first" },
                new { simple_prop = "second" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first!second!", docText);
    }

    [Fact]
    public async Task SimpleParagraphLoops_ListOfDictionaries()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");

        var data = new Dictionary<string, object?>
        {
            ["loop_prop"] = new List<Dictionary<string, object?>>
            {
                new() { ["simple_prop"] = "first" },
                new() { ["simple_prop"] = "second" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first!second!", docText);
    }

    [Fact]
    public async Task SimpleParagraphLoops_Json()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - simple.docx");

        var data = System.Text.Json.Nodes.JsonNode.Parse("""
            { "loop_prop": [ { "simple_prop": "first" }, { "simple_prop": "second" } ] }
            """);

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first!second!", docText);
    }

    [Fact]
    public async Task SimpleTableRowLoops()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop}Repeat this text {prop} And this also…{/loop}", templateText.Trim());

        var data = new
        {
            outProp = "I am out!",
            loop = new[]
            {
                new { prop = "first" },
                new { prop = "second" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("Repeat this text first And this also…Repeat this text second And this also…", docText);

        var docXml = await handler.GetXmlAsync(doc);
        Assert.Equal(2, docXml!.Descendants(W + "tr").Count());
    }

    [Fact]
    public async Task SimpleListLoops()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - list.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop1}Hi{#loop2}{prop}{/loop2}{/loop1}", templateText.Trim());

        var data = new
        {
            loop1 = new[]
            {
                new { loop2 = new[] { new { prop = "first" }, new { prop = "second" } } },
                new { loop2 = new[] { new { prop = "third" }, new { prop = "forth" } } }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("HifirstsecondHithirdforth", docText);
    }

    [Fact]
    public async Task CustomLoopDelimiters()
    {
        var handler = new TemplateHandler(new TemplateHandlerOptions
        {
            Delimiters = new Delimiters
            {
                ContainerTagOpen = ">>>",
                ContainerTagClose = "<<<"
            }
        });
        var template = ReadFixture("loop - custom delimiters.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{>>> loop_prop}{simple_prop}!{<<< loop_prop}", templateText.Trim());

        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop = "first" },
                new { simple_prop = "second" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first!second!", docText);
    }

    [Fact]
    public async Task BooleanConditions()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - conditions.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop_prop1}hi!{#condition1}yes!{/}{#condition2}no!{/}{/}", templateText.Trim());

        var data = new
        {
            loop_prop1 = new[]
            {
                new { condition1 = true, condition2 = false },
                new { condition1 = false, condition2 = true },
                new { condition1 = false, condition2 = false },
                new { condition1 = true, condition2 = true }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hi!yes!hi!no!hi!hi!yes!no!", docText);
    }

    [Fact]
    public async Task BooleanConditionsInLists()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - list - conditions.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("Point of contact: {#isLegal}legal@abc.com{/}{#isMarketing}marketing@abc.com{/}", templateText.Trim());

        var data = new { isLegal = true, isMarketing = false };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("Point of contact: legal@abc.com", docText);
    }

    [Fact]
    public async Task IgnoreClosingTagName()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - nested - ignore closing tag name.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop_prop1}hi!{#loop_prop2}{simple_prop}!{/some_name}{/}", templateText.Trim());

        var data = new
        {
            loop_prop1 = new[]
            {
                new { loop_prop2 = new[] { new { simple_prop = "first" }, new { simple_prop = "second" } } },
                new { loop_prop2 = new[] { new { simple_prop = "third" }, new { simple_prop = "forth" } } }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hi!first!second!hi!third!forth!", docText);
    }

    [Fact]
    public async Task MultiplePropsInTheSameIteration()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - multi props.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop_prop}{simple_prop1}!{simple_prop2}!{simple_prop3}!{/loop_prop}", templateText.Trim());

        var data = new
        {
            loop_prop = new[]
            {
                new { simple_prop1 = "first", simple_prop2 = "second", simple_prop3 = "third" },
                new { simple_prop1 = "forth", simple_prop2 = "fifth", simple_prop3 = "sixth" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("first!second!third!forth!fifth!sixth!", docText);
    }

    [Fact]
    public async Task SameLineLoop()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - same line.docx");
        var templateText = await handler.GetTextAsync(template);

        var data = new
        {
            loop = new[] { new { val = "1" }, new { val = "2" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.DoesNotContain("{", docText);
        Assert.NotEqual(templateText, docText);
    }

    //
    // nested
    //

    [Fact]
    public async Task SimpleNestedLoops()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - nested.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{#loop_prop1}hi!{#loop_prop2}{simple_prop}!{/loop_prop2}{/loop_prop1}", templateText.Trim());

        var data = new
        {
            loop_prop1 = new[]
            {
                new { loop_prop2 = new[] { new { simple_prop = "first" }, new { simple_prop = "second" } } },
                new { loop_prop2 = new[] { new { simple_prop = "third" }, new { simple_prop = "forth" } } }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("hi!first!second!hi!third!forth!", docText);
    }

    [Fact]
    public async Task SimpleNestedConditions()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - condition in condition.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{# condition a}{# condition b}{yes}{/}{# condition c}{no}{/}{/}", templateText.Trim());

        var data = new Dictionary<string, object?>
        {
            ["condition a"] = true,
            ["condition b"] = true,
            ["condition c"] = false,
            ["yes"] = "Yes!",
            ["no"] = "Oh no!",
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("Yes!", docText);
    }

    [Fact]
    public async Task ALoopInsideAConditionInsideALoop()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - nested with condition.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal("{# teams}{# display}{name}:{# members}First Name: {name}, Last Name: {surname}{/ members};{/}{/ teams}", templateText.Trim());

        var data = new
        {
            teams = new[]
            {
                new
                {
                    display = true,
                    name = "Team A",
                    members = new[] { new { name = "Bryson", surname = "Pike" }, new { name = "Beatriz", surname = "Schmitt" } }
                },
                new
                {
                    display = false,
                    name = "Team B",
                    members = new[] { new { name = "Charis", surname = "Hilton" }, new { name = "John", surname = "Plant" } }
                },
                new
                {
                    display = true,
                    name = "Team C",
                    members = new[] { new { name = "Fionn", surname = "Lyons" }, new { name = "Pedro", surname = "Compton" } }
                }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("Team A:First Name: Bryson, Last Name: Pike;First Name: Beatriz, Last Name: Schmitt;Team C:First Name: Fionn, Last Name: Lyons;First Name: Pedro, Last Name: Compton;", docText);
    }

    [Fact]
    public async Task NestedLoopsReplacementIsFastEnough()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - nested with image.docx");

        // generate lots of data
        const int maxOuterLoop = 1000;
        const int maxInnerLoop = 20;
        var outer = new List<object>();
        for (var i = 0; i < maxOuterLoop; i++)
        {
            var inner = new List<object>();
            for (var j = 0; j < maxInnerLoop; j++)
                inner.Add(new { simple_prop = (i * maxOuterLoop + j).ToString() });
            outer.Add(new { loop_prop2 = inner });
        }
        var data = new { loop_prop1 = outer };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await handler.ProcessAsync(template, data);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Took {stopwatch.Elapsed}");
    }

    //
    // paragraph
    //

    [Fact]
    public async Task SingleLine_NoLoopOverOption()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - paragraph - one line - without loopOver.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace("Before {#loop_prop} middle1 {simple_prop} middle2 {/loop_prop} after"), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop_prop = new[] { new { simple_prop = "FIRST" }, new { simple_prop = "SECOND" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace("Before middle1 FIRST middle2 middle1 SECOND middle2 after"), RemoveWhiteSpace(docText));
    }

    [Fact]
    public async Task SingleLine_LoopOverParagraph()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - paragraph - one line - with loopOver.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace("Before {#loop_prop [loopOver: “paragraph”]} middle1 {simple_prop} middle2 {/loop_prop} after"), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop_prop = new[] { new { simple_prop = "FIRST" }, new { simple_prop = "SECOND" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Before middle1 FIRST middle2 after
            Before middle1 SECOND middle2 after
        "), RemoveWhiteSpace(docText));
    }

    [Fact]
    public async Task MultiLine_NoLoopOverOption()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - paragraph - multi line - without loopOver.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            Before1 {#loop_prop} After1
            Before2 {simple_prop} After2
            Before3 {/loop_prop} After3
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop_prop = new[] { new { simple_prop = "FIRST" }, new { simple_prop = "SECOND" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Before1 After1
            Before2 FIRST After2
            Before3 After1
            Before2 SECOND After2
            Before3 After3
        "), RemoveWhiteSpace(docText));
    }

    [Fact]
    public async Task MultiLine_LoopOverParagraph()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - paragraph - multi line - with loopOver.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            Before1 {#loop_prop [loopOver: “paragraph”]} After1
            Before2 {simple_prop} After2
            Before3 {/loop_prop} After3
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop_prop = new[] { new { simple_prop = "FIRST" }, new { simple_prop = "SECOND" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Before1 After1
            Before2 FIRST After2
            Before3 After3
            Before1 After1
            Before2 SECOND After2
            Before3 After3
        "), RemoveWhiteSpace(docText));
    }

    [Fact]
    public async Task TableCellsWithLoopOverParagraph()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table - loopOver paragraph.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            Hello1 {#loop1 [loopOver: “paragraph”]}{val}{/loop1} World1
            Hello2 {#loop1 [loopOver: “paragraph”]}{val}{/loop1} World2
            Hello3 {#loop1 [loopOver: “paragraph”]}{val}{/loop1} World3
        "), RemoveWhiteSpace(templateText));

        var data = new { loop1 = Array.Empty<object>() };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Hello1 World1
            Hello2 World2
            Hello3 World3
        "), RemoveWhiteSpace(docText));

        // table cells must not be left empty
        var docXml = await handler.GetXmlAsync(doc);
        foreach (var cell in docXml!.Descendants(W + "tc"))
            Assert.True(cell.Elements(W + "p").Any() || cell.Elements(W + "tbl").Any(), "Table cell must contain a paragraph.");
    }

    //
    // table
    //

    [Fact]
    public async Task TableLoopOverOption()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table - loopOver.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            [Row1]{#loop1}{val}{/loop1}{#loop2}{val}{/loop2}
            [Row2]{#loop3[loopOver:“row”]}{val}{/loop3}
            [Row3]{#loop4[loopOver:“content”]}{val}{/loop4}
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop1 = new[] { new { val = "val1" }, new { val = "val2" } },
            loop2 = new[] { new { val = "val3" }, new { val = "val4" } },
            loop3 = new[] { new { val = "val5" }, new { val = "val6" } },
            loop4 = new[] { new { val = "val7" }, new { val = "val8" } }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal("[Row1]val1val2val3val4[Row2]val5[Row2]val6[Row3]val7val8", docText);
    }

    [Fact]
    public async Task TableColumnLoop()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table - columns.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            Static 1    {#loop}{prop1}  Static 4
            Static 2    Repeat me
                        {prop2}         Static 5
            Static 3    {/loop}         Static 6
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            loop = new[]
            {
                new { prop1 = "Dynamic 1", prop2 = "Dynamic 2" },
                new { prop1 = "Dynamic 3", prop2 = "Dynamic 4" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Static 1    Dynamic 1   Dynamic 3   Static 4
            Static 2    Repeat me   Repeat me
                        Dynamic 2   Dynamic 4   Static 5
            Static 3                            Static 6
        "), RemoveWhiteSpace(docText));

        var docXml = await handler.GetXmlAsync(doc);
        foreach (var row in docXml!.Descendants(W + "tr"))
            Assert.Equal(4, row.Elements(W + "tc").Count());
    }

    [Fact]
    public async Task StyledTableColumnLoop()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table - columns with style.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            Student	{#Students}{Name}
            Math	{MathGrade}
            Science	{ScienceGrade}
            History	{HistoryGrade}{/}
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            Students = new[]
            {
                new { Name = "John Doe", MathGrade = 10, ScienceGrade = 11, HistoryGrade = 12 },
                new { Name = "Jane Smith", MathGrade = 20, ScienceGrade = 21, HistoryGrade = 22 }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            Student    John Doe    Jane Smith
            Math       10          20
            Science    11          21
            History    12          22
        "), RemoveWhiteSpace(docText));
    }

    [Fact]
    public async Task MergedCells()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - table - merged cells.docx");
        var templateText = await handler.GetTextAsync(template);
        Assert.Equal(RemoveWhiteSpace(@"
            {#list}{no}	Details
                        Name:	    {name}
                        Surname:	{surname}{/list}
        "), RemoveWhiteSpace(templateText));

        var data = new
        {
            list = new[]
            {
                new { no = 1, name = "First 1", surname = "Last 1" },
                new { no = 2, name = "First 2", surname = "Last 2" }
            }
        };

        var doc = await handler.ProcessAsync(template, data);
        var docText = await handler.GetTextAsync(doc);
        Assert.Equal(RemoveWhiteSpace(@"
            1	Details
            	Name:	    First 1
            	Surname:	Last 1
            2	Details
            	Name:	    First 2
            	Surname:	Last 2
        "), RemoveWhiteSpace(docText));
    }

    //
    // data types
    //

    [Fact]
    public async Task ConditionsSupportVariousTruthyValues()
    {
        var handler = new TemplateHandler();
        var template = ReadFixture("loop - list - conditions.docx");

        var doc = await handler.ProcessAsync(template, new { isLegal = 1, isMarketing = 0 });
        Assert.Equal("Point of contact: legal@abc.com", await handler.GetTextAsync(doc));

        doc = await handler.ProcessAsync(template, new { isLegal = "", isMarketing = "yes" });
        Assert.Equal("Point of contact: marketing@abc.com", await handler.GetTextAsync(doc));

        doc = await handler.ProcessAsync(template, new { isLegal = (object?)null, isMarketing = new { } });
        Assert.Equal("Point of contact: marketing@abc.com", await handler.GetTextAsync(doc));

        doc = await handler.ProcessAsync(template, new Dictionary<string, object?> { ["isLegal"] = true });
        Assert.Equal("Point of contact: legal@abc.com", await handler.GetTextAsync(doc));
    }
}
