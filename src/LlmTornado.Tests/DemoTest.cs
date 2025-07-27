using System.Reflection;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;
using LlmTornado.Demo;
using Assert = NUnit.Framework.Assert;

namespace LlmTornado.Tests;

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
        public string? FlakyReason { get; set; }
    }

    private static IEnumerable<TestCaseData> DemoCases()
    {
        foreach (KeyValuePair<string, Program.TestRun> mi in Program.DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCulture))
        {
            TestCaseData testCase = new TestCaseData(string.Empty, new GeneratedTestCase
            {
                Fn = () => (Task)mi.Value.Method.Invoke(null, mi.Value.Arguments),
                Flaky = mi.Value.Flaky is not null,
                FlakyReason = mi.Value.Flaky?.Reason
            }) 
            {
                TestName = mi.Value.Name
            };
                
            yield return testCase;
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
                Assert.Ignore(method.FlakyReason ?? "Flaky test skipped");
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
            Assert.Fail($"Test failed with exception: {e.Message}\nStackTrace: \n{e.StackTrace}");
        }
    }
}