namespace LlmTornado.A2A.Hosting.Docker;

public class DockerComposeBuilder
{
    public string DockerComposeYaml { get; set; } = "";
    public string CurrentFilePath { get; set; } = "";

    public DockerComposeBuilder()
    {
        DockerComposeYaml = "version: '3.8'\r\nservices:\r\n";
    }
    public DockerComposeBuilder AddService(DockerContainerOptionsBuilder serviceYaml)
    {
        DockerComposeYaml += serviceYaml.ComposeCommand + "\r\n";
        return this;
    }

    public DockerComposeBuilder AddNetwork(string networkName)
    {
        DockerComposeYaml += $"networks:\r\n  {networkName}:\r\n    driver: bridge\r\n";
        return this;
    }

    public DockerComposeBuilder AddChromaService()
    {
        DockerComposeYaml +=
            $"  Chroma:\r\n" +
            $"    image: chromadb/chroma\r\n" +
            $"    ports:\r\n" +
            $"      - \"8000:8000\"\r\n";

        return this;
    }

    public DockerComposeBuilder SaveToFile(string filePath)
    {
        CurrentFilePath = filePath;
        System.IO.File.WriteAllText(filePath, DockerComposeYaml);
        return this;
    }
}

public class DockerContainerOptionsBuilder
{
    public string ComposeCommand { get; set; } = "";

    public DockerContainerOptionsBuilder BuildDockerComposeYaml(
        string containerName,
        string imageName,
        string version)
    {
        ComposeCommand = 
$@"  {containerName}:
    image: {imageName}
";
        return this;
    }

    public DockerContainerOptionsBuilder AddVolumeMount(string hostPath, string containerPath)
    {
        ComposeCommand += $"    volumes:\r\n      - {hostPath}:{containerPath}\r\n";
        return this;
    }

    public DockerContainerOptionsBuilder AddPortMapping(string hostPort, string containerPort)
    {
        ComposeCommand += $"    ports:\r\n      - \"{hostPort}:{containerPort}\"\r\n";
        return this;
    }

    public DockerContainerOptionsBuilder AddEnvironmentVariables(Dictionary<string, string> environmentVariables)
    {
        if (environmentVariables == null || environmentVariables.Count == 0)
            return this;
        var envVars = string.Join("\r\n      ", environmentVariables.Select(ev => $"{ev.Key}: {ev.Value}"));
        ComposeCommand += $"    environment:\r\n      {envVars}";
        return this;
    }
}
