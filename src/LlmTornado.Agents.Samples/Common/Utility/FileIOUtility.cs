using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.Common.Utility;

public class FileIOUtility
{
    public static string? SafeWorkingDirectory { get; set; }

    public static string ReadFile(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(FileIOUtility.SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            filePath = filePath.Trim();

            Path.GetInvalidFileNameChars().ToList().ForEach(c =>
            {
                if (Path.GetFileName(filePath).Contains(c))
                {
                    throw new ArgumentException($"File name cannot contain {c}");
                }
            }
            );
            // Validate the file path
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.");
            }
            if (filePath.StartsWith("..") || filePath.StartsWith("/"))
            {
                throw new ArgumentException("File path cannot contain relative paths like '..', or '/'.");
            }
            if (filePath.StartsWith("\\"))
            {
                filePath = filePath.TrimStart('\\');
            }
            string fixedPath = Path.Combine(FileIOUtility.SafeWorkingDirectory, filePath.Trim());

            return File.ReadAllText(fixedPath);
        }
        catch (Exception ex)
        {
            return $"Error reading file -> {ex.Message}"; // Return empty string or handle as needed
        }
    }

    public static void WriteFile(string filePath, string content)
    {

        if (string.IsNullOrEmpty(SafeWorkingDirectory))
        {
            throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
        }

        string FixedPath = Path.Combine(SafeWorkingDirectory, filePath);

        string? directoryPath = Path.GetDirectoryName(FixedPath);

        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(FixedPath, content);
    }

    public static string GetAllPaths(string directory)
    {
        if (string.IsNullOrEmpty(SafeWorkingDirectory))
        {
            throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
        }

        List<string> allPaths = new List<string>();

        GetPaths(directory, allPaths);

        for (int i = 0; i < allPaths.Count; i++)
        {
            allPaths[i] = allPaths[i].Replace(SafeWorkingDirectory, "");
        }

        foreach (string path in allPaths)
        {
            Console.WriteLine(path);
        }

        return string.Join(Environment.NewLine, allPaths);
    }

    public static void GetPaths(string directory, List<string> paths)
    {
        if (string.IsNullOrEmpty(SafeWorkingDirectory))
        {
            throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
        }

        string SafePath = Path.Combine(SafeWorkingDirectory, directory);

        try
        {
            // Add files in the current directory
            paths.AddRange(Directory.GetFiles(SafePath));

            // Add subdirectories in the current directory and recurse
            string[] subdirectories = Directory.GetDirectories(SafePath);
            //paths.AddRange(subdirectories);
            foreach (string subdirectory in subdirectories)
            {
                if (subdirectory.Contains("bin")) continue;
                if (subdirectory.Contains("obj")) continue;
                GetPaths(subdirectory, paths);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Access denied to: {ex.Message}"); // Handle exceptions gracefully
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"Directory not found: {ex.Message}"); // Handle exceptions gracefully
        }
    }
}




