using System.Diagnostics;

namespace LlmTornado.Demo;

public static class Helpers
{
    public static async Task<bool> ProgramExists(string name)
    {
        bool exists;
        
        try 
        {
            using Process checkProcess = new Process();
            checkProcess.StartInfo = new ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which",
                Arguments = name,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            checkProcess.Start();
            string output = await checkProcess.StandardOutput.ReadToEndAsync();
            await checkProcess.WaitForExitAsync();
            
            exists = checkProcess.ExitCode == 0 && !string.IsNullOrEmpty(output);
        }
        catch
        {
            exists = false;
        }

        return exists;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TornadoTestAttribute : Attribute
{
    public string? FriendlyName { get; set; }

    public TornadoTestAttribute(string? friendlyName = null)
    {
        FriendlyName = friendlyName;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TornadoTestCaseAttribute : Attribute
{
    public object[] Arguments { get; }
    
    public TornadoTestCaseAttribute(params object[] arguments)
    {
        Arguments = arguments;
    }
}