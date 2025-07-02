using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace LlmTornado.TestAdapter
{
    [FileExtension(".dll")]
    [DefaultExecutorUri(TornadoTestExecutor.ExecutorUriString)]
    public class TornadoTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            // Log that your discoverer was actually called
            logger.SendMessage(TestMessageLevel.Informational, "TornadoTestDiscoverer: Starting discovery.");

            foreach (string source in sources)
            {
                logger.SendMessage(TestMessageLevel.Informational, $"TornadoTestDiscoverer: Processing source '{source}'.");
                Assembly assembly;
                
                try
                {
                    // Use a more robust loading context if you face file locking issues, but LoadFrom is fine for now.
                    assembly = Assembly.LoadFrom(source);
                }
                catch (Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Warning, $"TornadoTestDiscoverer: Failed to load assembly '{source}'. Exception: {ex}");
                    continue;
                }
                
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (method.GetCustomAttribute<TornadoTestAttribute>(false) != null)
                            {
                                logger.SendMessage(TestMessageLevel.Informational, $"TornadoTestDiscoverer: Found test '{type.FullName}.{method.Name}'.");
                                TestCase testCase = new TestCase(
                                    $"{type.FullName}.{method.Name}",
                                    new Uri(TornadoTestExecutor.ExecutorUriString),
                                    source)
                                {
                                    DisplayName = method.Name
                                };
                                discoverySink.SendTestCase(testCase);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Error, $"TornadoTestDiscoverer: Error while scanning types in '{source}'. Exception: {ex}");
                }
            }
            logger.SendMessage(TestMessageLevel.Informational, "TornadoTestDiscoverer: Discovery finished.");
        }
    }

    [ExtensionUri(ExecutorUriString)]
    public class TornadoTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://TornadoTestExecutor";

        private static CancellationTokenSource? _cts;

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _cts = new CancellationTokenSource();
            
            CancellationToken token = _cts.Token;
            foreach (TestCase test in tests)
            {
                if (token.IsCancellationRequested)
                    break;
                TestResult result = new TestResult(test);
                try
                {
                    Assembly assembly = Assembly.LoadFrom(test.Source);
                    string typeName = test.FullyQualifiedName.Substring(0, test.FullyQualifiedName.LastIndexOf('.'));
                    string methodName = test.FullyQualifiedName.Substring(test.FullyQualifiedName.LastIndexOf('.') + 1);
                    Type? type = assembly.GetType(typeName);
                    MethodInfo? method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                    Task? task = method.Invoke(null, null) as Task;
                    task?.GetAwaiter().GetResult();
                    result.Outcome = TestOutcome.Passed;
                }
                catch (Exception ex)
                {
                    result.Outcome = TestOutcome.Failed;
                    result.ErrorMessage = ex.ToString();
                }
                frameworkHandle.RecordResult(result);
            }
            _cts = null;
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            TornadoTestDiscoverer discoverer = new TornadoTestDiscoverer();
            TestCaseCollector sink = new TestCaseCollector();
            discoverer.DiscoverTests(sources, runContext, frameworkHandle, sink);
            RunTests(sink.TestCases, runContext, frameworkHandle);
        }

        private class TestCaseCollector : ITestCaseDiscoverySink
        {
            public List<TestCase> TestCases { get; } = new List<TestCase>();
            public void SendTestCase(TestCase testCase) => TestCases.Add(testCase);
        }
    }
}
