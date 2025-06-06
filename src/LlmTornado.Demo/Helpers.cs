using System.Diagnostics;

namespace LlmTornado.Demo;

public static class Helpers
{
    public static async Task<bool> ProgramExists(string name)
    {
        bool exists = false;
        
        try 
        {
            // Zkusíme zjistit, zda je chafa dostupný v cestě
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