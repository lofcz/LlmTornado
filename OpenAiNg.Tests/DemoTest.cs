using OpenAiNg.Demo;

namespace OpenAiNg.Tests;

public class Tests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    public static IEnumerable<TestCaseData> DemoCases()
    {
        foreach (Demos demo in Enum.GetValues<Demos>())
        {
            Func<Task>? task = Program.GetDemo(demo);

            if (task is not null)
            {
                TestCaseData testCase = new TestCaseData(string.Empty, task) 
                {
                    TestName = task.Method.Name
                };
                
                yield return testCase;
            }
        }
    }
    
    [Test]
    [TestCaseSource(nameof(DemoCases))]
    public async Task TestDemos(object _, Func<Task> method)
    {
        try
        {
            await method.Invoke();
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
}