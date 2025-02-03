using System.Reflection;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;
using LlmTornado.Demo;

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
        foreach (KeyValuePair<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> mi in Program.DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCulture))
        {
            TestCaseData testCase = new TestCaseData(string.Empty, new GeneratedTestCase
            {
                Fn = () => (Task)mi.Value.Item1.Invoke(null, null),
                Flaky = mi.Value.Item4 is not null,
                FlakyReason = mi.Value.Item4?.Reason
            }) 
            {
                TestName = $"{mi.Value.Item3.Name} - {mi.Value.Item1.Name}"
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
            Assert.Fail(e.Message);
        }
    }
}