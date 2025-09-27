using System.ComponentModel;

namespace LlmTornado.A2A.AgentServer.SampleAgent.ComplexAgent;

public class ComplexAgentTools
{
    [Description("Use this to run Shell commands")]
    public static string ExecuteLinuxCommand(
        [Description("The Linux command to execute in the following code: new System.Diagnostics.ProcessStartInfo(\"bash\", $\"-c \\\"{command}\\\"\")")] string command,
        [Description("The Linux command timeout in milliseconds (default 20 seconds) raise or lower as needed")] int timeoutMilliseconds = 20000)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo("bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(timeoutMilliseconds); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error executing command: {error}";
            }
            return output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }

}


public class GitHubTool
{
    public string RepoUrl { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string SavePath { get; set; } = "/app/output";
    public string LocalPath  => Path.Combine(SavePath, RepoName);
    public string BranchName { get; set; } = $"agent_update_{Guid.NewGuid()}";

    [Description("Use this function to create a new git repo")]
    public string CreateGitRepository([Description("name of the new repo")] string repoName)
    {
        RepoName = repoName;
        if (Directory.Exists(LocalPath))
        {
            return "Repository already exists.";
        }
        try
        {
            Directory.CreateDirectory(LocalPath);
            var processInfo = new System.Diagnostics.ProcessStartInfo("git", "init")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = LocalPath
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error creating repository: {error}";
            }
            return "Repository created at " + LocalPath + "\n" + output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }


    [Description("Use this function to download github repos to a local directory")]
    public string CloneGithubRepository(
        [Description("The HTTPS URL of the GitHub repository to clone, e.g., https://github.com/user/repo.git")] string repoUrl)
    {
        RepoUrl = repoUrl;
        RepoName = repoUrl.Split('/').Last().Replace(".git", "");
        if(Directory.Exists(LocalPath))
        {
            return "Repository already cloned.";
        }
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo("git", $"clone {repoUrl} {SavePath}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error cloning repository: {error}";
            }
            return "Saved to /app/output " + output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }

    [Description("Use this function to create a branch of a repo")]
    public string CreateBranchOfRepo(
        [Description("local path to the git repo, e.g., /app/output")] string localRepoPath,
        [Description("The name of the new branch to create, e.g., feature-branch")] string branchName)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo("git", $"checkout -b {branchName}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = localRepoPath
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error creating branch: {error}";
            }
            return "Branch created: " + output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }

    [Description("Use this function commit and push changes to the remote repository")]
    public string CommitAndPushChanges(
        [Description("local path to the git repo, e.g., /app/output")] string localRepoPath,
        [Description("The commit message for the changes, e.g., 'Updated README'")] string commitMessage,
        [Description("The name of the branch to push to, e.g., feature-branch")] string branchName,
        [Description("GitHub username for authentication")] string username,
        [Description("GitHub personal access token for authentication")] string token)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo("bash", $"-c \"git add . && git commit -m \\\"{commitMessage}\\\" && git push https://{username}:{token}@github.com/user/repo.git {branchName}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = localRepoPath
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error committing and pushing changes: {error}";
            }
            return "Changes committed and pushed: " + output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }

    [Description("Use this function to commit and push changes to a local repo")]
    public  string LocalCommitAndPushChanges([Description("The commit message for the changes, e.g., 'Updated README'")] string commitMessage)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo("bash", $"-c \"git add . && git commit -m \\\"{commitMessage}\\\" && git push origin {BranchName}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = LocalPath
            };
            using var process = new System.Diagnostics.Process
            {
                StartInfo = processInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000); // Wait up to 20 seconds for the process to exit
            if (process.ExitCode != 0)
            {
                return $"Error committing and pushing changes: {error}";
            }
            return "Changes committed and pushed: " + output + "\n" + error;
        }
        catch (Exception ex)
        {
            return $"Exception occurred: {ex.Message}";
        }
    }
}
