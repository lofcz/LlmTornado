using OpenAiNg.Demo;

namespace OpenAiNg.Tests;

public class Tests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    public class GeneratedTestCase
    {
        public Func<Task> Fn { get; set; }
        public bool Flaky { get; set; }
    }

    public static IEnumerable<TestCaseData> DemoCases()
    {
        foreach (Demos demo in Enum.GetValues<Demos>())
        {
            Func<Task>? task = Program.GetDemo(demo);
            
            if (task is not null)
            {
                TestCaseData testCase = new TestCaseData(string.Empty, new GeneratedTestCase
                {
                    Fn = task,
                    Flaky = demo.GetType().GetField(demo.ToString())?.GetCustomAttributes(typeof(FlakyAttribute), false).Length > 0
                }) 
                {
                    TestName = $"{demo} - {task.Method.Name}"
                };
                
                yield return testCase;
            }
        }
    }
    
    [Test]
    [TestCaseSource(nameof(DemoCases))]
    public async Task TestDemos(object _, GeneratedTestCase method)
    {
        try
        {
            if (method.Flaky)
            {
                Assert.Ignore("Flaky test skipped");
                return;
            }

            await method.Fn.Invoke();
        }
        catch (SuccessException se)
        {
            
        }
        catch (IgnoreException ie)
        {
            Assert.Inconclusive(ie.Message);
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
}