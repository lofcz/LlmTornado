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
        foreach (Tuple<Type, Type> type in Program.DemoEnumTypes.OrderBy(x => x.Item1.Name, StringComparer.InvariantCulture))
        {
            foreach (object? eVal in Enum.GetValues(type.Item1))
            {
                List<string> keys = Program.DemoDict.Select(x => x.Key).OrderBy(x => x, StringComparer.InvariantCulture).ToList();
                
                object[] attrs = eVal.GetType().GetField(eVal.ToString()).GetCustomAttributes(typeof(MethodAttribute), false);

                if (attrs.Length > 0 && attrs[0] is MethodAttribute ma)
                {
                    if (Program.DemoDict.TryGetValue($"{type.Item2.FullName}.{ma.MethodName}", out MethodInfo? mi))
                    {
                        FlakyAttribute[]? flakyAttrs = (FlakyAttribute[]?)eVal.GetType().GetField(eVal.ToString())?.GetCustomAttributes(typeof(FlakyAttribute), false);
                    
                        TestCaseData testCase = new TestCaseData(string.Empty, new GeneratedTestCase
                        {
                            Fn = () => (Task)mi.Invoke(null, null),
                            Flaky = flakyAttrs?.Length > 0,
                            FlakyReason = flakyAttrs?.Length > 0 ? flakyAttrs[0].Reason : null
                        }) 
                        {
                            TestName = $"{type.Item1.Name} - {mi.Name}"
                        };
                
                        yield return testCase;
                    }   
                }
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