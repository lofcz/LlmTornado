using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Codex;

internal interface ICodexAppServerTransport : IDisposable
{
    IReadOnlyCollection<string> RecentStandardError { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
    Task WriteLineAsync(string line, CancellationToken cancellationToken);
    Task StopAsync();
}

internal sealed class CodexProcessTransport : ICodexAppServerTransport
{
    private const int StandardErrorLimit = 400;
    private readonly CodexAppServerOptions options;
    private readonly ConcurrentQueue<string> standardError = new ConcurrentQueue<string>();
    private Process? process;
    private Task? standardErrorTask;

    internal CodexProcessTransport(CodexAppServerOptions options)
    {
        this.options = options;
    }

    public IReadOnlyCollection<string> RecentStandardError => standardError.ToArray();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(options.ExecutablePath),
            Arguments = BuildArguments(options.ConfigOverrides),
            WorkingDirectory = options.WorkingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        foreach (KeyValuePair<string, string> variable in options.EnvironmentVariables)
        {
            startInfo.EnvironmentVariables[variable.Key] = variable.Value;
        }

        process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start Codex executable '{options.ExecutablePath}'.");
            }
        }
        catch (Exception exception)
        {
            process.Dispose();
            process = null;
            throw new InvalidOperationException(
                $"Unable to start Codex app-server using '{options.ExecutablePath}'. Install Codex or set {nameof(CodexAppServerOptions.ExecutablePath)}.",
                exception);
        }

        process.StandardInput.AutoFlush = true;
        standardErrorTask = DrainStandardErrorAsync(process.StandardError);
        return Task.CompletedTask;
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        Process activeProcess = GetProcess();
        return await CodexTask.WithCancellation(activeProcess.StandardOutput.ReadLineAsync(), cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Process activeProcess = GetProcess();
        await activeProcess.StandardInput.WriteLineAsync(line).ConfigureAwait(false);
        await activeProcess.StandardInput.FlushAsync().ConfigureAwait(false);
    }

    public Task StopAsync()
    {
        Process? activeProcess = process;
        process = null;

        if (activeProcess is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            activeProcess.StandardInput.Close();
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            if (!activeProcess.HasExited)
            {
                activeProcess.Kill();
                activeProcess.WaitForExit(2_000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            activeProcess.Dispose();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private Process GetProcess()
        => process ?? throw new InvalidOperationException("The Codex app-server process is not running.");

    private async Task DrainStandardErrorAsync(StreamReader reader)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                standardError.Enqueue(line);

                while (standardError.Count > StandardErrorLimit)
                {
                    standardError.TryDequeue(out _);
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static string BuildArguments(IEnumerable<string> configOverrides)
    {
        List<string> arguments = [];

        foreach (string configOverride in configOverrides)
        {
            arguments.Add("--config");
            arguments.Add(configOverride);
        }

        arguments.Add("app-server");
        arguments.Add("--listen");
        arguments.Add("stdio://");

        return string.Join(" ", arguments.ConvertAll(QuoteArgument));
    }

    private static string ResolveExecutablePath(string executablePath)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return executablePath;
        }

        bool containsDirectory = executablePath.IndexOf(Path.DirectorySeparatorChar) >= 0
                                 || executablePath.IndexOf(Path.AltDirectorySeparatorChar) >= 0;

        if (containsDirectory && !Path.IsPathRooted(executablePath))
        {
            executablePath = Path.GetFullPath(executablePath);
        }

        if (Path.IsPathRooted(executablePath) && string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return executablePath;
        }

        IEnumerable<string> searchDirectories;

        if (Path.IsPathRooted(executablePath))
        {
            searchDirectories = [Path.GetDirectoryName(executablePath) ?? string.Empty];
        }
        else
        {
            searchDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .Where(directory => !string.IsNullOrWhiteSpace(directory));
        }

        string executableName = Path.GetFileNameWithoutExtension(executablePath);

        foreach (string directory in searchDirectories)
        {
            string directExecutable = Path.Combine(directory, executableName + ".exe");

            if (File.Exists(directExecutable))
            {
                return directExecutable;
            }

            foreach (string nativeExecutable in GetNpmNativeExecutableCandidates(directory))
            {
                if (File.Exists(nativeExecutable))
                {
                    return nativeExecutable;
                }
            }
        }

        throw new FileNotFoundException(
            $"Unable to resolve a native Codex executable from '{executablePath}'. Set {nameof(CodexAppServerOptions.ExecutablePath)} to codex.exe.",
            executablePath);
    }

    private static IEnumerable<string> GetNpmNativeExecutableCandidates(string npmDirectory)
    {
        string packagesRoot = Path.Combine(npmDirectory, "node_modules", "@openai");
        string nestedPackagesRoot = Path.Combine(packagesRoot, "codex", "node_modules", "@openai");

        foreach (string packageRoot in new[] { nestedPackagesRoot, packagesRoot })
        {
            yield return Path.Combine(packageRoot, "codex-win32-x64", "vendor", "x86_64-pc-windows-msvc", "bin", "codex.exe");
            yield return Path.Combine(packageRoot, "codex-win32-arm64", "vendor", "aarch64-pc-windows-msvc", "bin", "codex.exe");
        }
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0)
        {
            return argument;
        }

        StringBuilder quoted = new StringBuilder(argument.Length + 2);
        quoted.Append('"');
        int backslashCount = 0;

        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                quoted.Append('\\', backslashCount * 2 + 1);
                quoted.Append(character);
                backslashCount = 0;
                continue;
            }

            quoted.Append('\\', backslashCount);
            quoted.Append(character);
            backslashCount = 0;
        }

        quoted.Append('\\', backslashCount * 2);
        quoted.Append('"');
        return quoted.ToString();
    }
}

internal static class CodexTask
{
    internal static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return await task.ConfigureAwait(false);
        }

        TaskCompletionSource<bool> cancellation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancellation))
        {
            if (task != await Task.WhenAny(task, cancellation.Task).ConfigureAwait(false))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return await task.ConfigureAwait(false);
    }
}
