using LlmTornado.A2A.Hosting.Models;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;


namespace LlmTornado.A2A.Hosting.Services;

public class DockerDispatchService: IA2ADispatchService
{
    private Dictionary<string, ServerInfo> _containers = new Dictionary<string, ServerInfo>();
    private readonly Dictionary<string,string> _agentImages;
    private readonly IConfiguration _configuration;

    public DockerDispatchService(IConfiguration configuration)
    {
        _configuration = configuration;
        _agentImages = new Dictionary<string,string>
        {
            { "ChatBot", "llmtornadoa2aagentserver:latest" }
        };
    }

    public async Task<ServerCreationResult> DispatchServerAsync(ServerCreationRequest request)
    {
        return await CreateContainerAsync(request);
    }

    public async Task<bool> RemoveServerAsync(string serverId)
    {
        return await RemoveContainerAsync(serverId);
    }

    public async Task<ServerStatus> GetServerStatusAsync(string serverId)
    {
        var status = await GetContainerStatusAsync(serverId);
        return status;
    }

    public string[] GetServerConfigurations()
    {
        return GetAvailableAgents();
    }

    public IEnumerable<ServerInfo> ListServers()
    {
        return GetActiveContainers();
    }

    public async Task<ServerCreationResult> CreateContainerAsync(ServerCreationRequest creationRequest)
    {
        try
        {
            // Generate unique container name
            var containerName = $"llmtornado-runtime-{Guid.NewGuid().ToString()}";
            var hostPort = await GetAvailablePortAsync();
            var containerPort = 8080;

            // Build docker run command
            var dockerArgs = new List<string>
            {
                "run", "-d",
                "--name", containerName,
                "-p", $"{hostPort}:{containerPort}",
            };

            foreach (var envVar in creationRequest.EnvironmentVariables ?? Array.Empty<string>())
            {
                dockerArgs.AddRange(new[] { "-e", envVar });
            }

            // Add volume mount if specified
            if (!string.IsNullOrEmpty(_configuration["Docker:MountPath"]))
            {
                var absoluteMountPath = Path.GetFullPath(_configuration["Docker:MountPath"]);
                Directory.CreateDirectory(absoluteMountPath); // Ensure directory exists
                dockerArgs.AddRange(new[] { "-v", $"{absoluteMountPath}:/app/output" });
            }

            dockerArgs.Add(_agentImages[creationRequest.AgentImageKey]);

            // Execute docker run command
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = string.Join(" ", dockerArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            //_logger.LogInformation("Starting container for runtime {RuntimeId}: {Command}", runtimeId, processInfo.Arguments);

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return new ServerCreationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start docker process"
                };
            }

            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode != 0)
            {
                //_logger.LogError("Docker container creation failed for runtime {RuntimeId}: {Error}", runtimeId, error);
                return new ServerCreationResult
                {
                    Success = false,
                    ErrorMessage = $"Docker command failed: {error}"
                };
            }

            var containerId = output.Trim();
            var endpoint = $"http://localhost:{hostPort}";

            // Store container info
            var containerInfo = new ServerInfo
            {
                ServerId = containerId,
                ServerConfiguration = containerName,
                HostPort = hostPort,
                RemotePort = containerPort,
                Endpoint = endpoint,
                MountPath = creationRequest.MountPath,
                CreatedAt = DateTime.UtcNow
            };

            _containers.Add(containerId, containerInfo);

            // Wait for container to be ready
            await WaitForContainerReadyAsync(endpoint, TimeSpan.FromSeconds(30));

            //_logger.LogInformation("Container created successfully for runtime {RuntimeId}: {ContainerId}", runtimeId, containerId);

            return new ServerCreationResult
            {
                Success = true,
                ServerId = containerId,
                Endpoint = endpoint
            };
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Failed to create container for runtime {RuntimeId}", runtimeId);
            return new ServerCreationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        try
        {
            if (!_containers.Remove(containerId, out var containerInfo))
            {
                //_logger.LogWarning("Container not found for runtime {RuntimeId}", runtimeId);
                return false;
            }

            // Stop and remove the container
            await ExecuteDockerCommandAsync($"stop {containerInfo.ServerId}");
            await ExecuteDockerCommandAsync($"rm {containerInfo.ServerId}");

            //_logger.LogInformation("Container removed for runtime {RuntimeId}: {ContainerId}", runtimeId, containerInfo.ContainerId);
            return true;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Failed to remove container for runtime {RuntimeId}", runtimeId);
            return false;
        }
    }

    public async Task<ServerStatus> GetContainerStatusAsync(string containerId)
    {
        try
        {
            if (!_containers.TryGetValue(containerId, out var containerInfo))
            {
                return new ServerStatus
                {
                    Status = "not_found",
                    IsHealthy = false,
                    ErrorMessage = "Container not found"
                };
            }

            // Check container status using docker inspect
            var output = await ExecuteDockerCommandAsync($"inspect {containerInfo.ServerId}");

            if (string.IsNullOrEmpty(output))
            {
                return new ServerStatus
                {
                    ServerId = containerInfo.ServerId,
                    Status = "unknown",
                    IsHealthy = false,
                    Endpoint = containerInfo.Endpoint,
                    ErrorMessage = "Failed to inspect container"
                };
            }

            // Parse docker inspect output (simplified)
            var isRunning = output.Contains("\"Running\": true");
            var status = isRunning ? "running" : "stopped";

            // Check health by trying to connect to the endpoint
            var isHealthy = isRunning && await CheckContainerHealthAsync(containerInfo.Endpoint);

            return new ServerStatus
            {
                ServerId = containerInfo.ServerId,
                Status = status,
                IsHealthy = isHealthy,
                Endpoint = containerInfo.Endpoint
            };
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Failed to get container status for runtime {RuntimeId}", runtimeId);
            return new ServerStatus
            {
                Status = "error",
                IsHealthy = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public string[] GetAvailableAgents()
    {
        return _agentImages.Keys.ToArray();
    }

    public string? GetContainerEndpoint(string containerId)
    {
        return _containers.TryGetValue(containerId, out var containerInfo) ? containerInfo.Endpoint : null;
    }

    public IEnumerable<ServerInfo> GetActiveContainers()
    {
        return _containers.Values;
    }

    private async Task<int> GetAvailablePortAsync()
    {
        // Simple port allocation - in production, this should be more sophisticated
        var random = new Random();
        var maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            var port = random.Next(10000, 65000);
            if (await IsPortAvailableAsync(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException("Could not find an available port");
    }

    private async Task<bool> IsPortAvailableAsync(int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync("localhost", port);
            return false; // Port is in use
        }
        catch
        {
            return true; // Port is available
        }
    }

    private async Task<string> ExecuteDockerCommandAsync(string arguments)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            int timeout = 10000; // 10 seconds timeout
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) => {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    // Process completed. Check process.ExitCode here.
                }
                else
                {
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Docker command failed: {error}");
                    }
                }
            }

            return output.ToString();
        }
       
    }

    private async Task WaitForContainerReadyAsync(string endpoint, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await client.GetAsync($"{endpoint}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Ignore connection errors while waiting
            }

            await Task.Delay(1000);
        }

        //_logger.LogWarning("Container at {Endpoint} did not become ready within {Timeout}", endpoint, timeout);
    }

    private async Task<bool> CheckContainerHealthAsync(string endpoint)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{endpoint}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

}

