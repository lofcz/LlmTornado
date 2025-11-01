using System.Diagnostics;
using System.Text;

namespace LlmTornado.Internal.Press.Services;

/// <summary>
/// Service for generating image variations (mipmaps) at different resolutions using smartcroppy
/// </summary>
public static class ImageVariationService
{
    private static bool? _smartcroppyAvailable = null;
    private static bool? _pipAvailable = null;
    private static readonly object _lockObject = new();

    public class ImageVariation
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Generates image variations (mipmaps) for the given source image
    /// Returns a dictionary of generated file paths keyed by "width_height"
    /// </summary>
    public static async Task<Dictionary<string, string>> GenerateVariationsAsync(
        string sourceImagePath,
        List<ImageVariation> variations,
        string logPrefix = "ImageVariation")
    {
        Dictionary<string, string> results = new Dictionary<string, string>();

        if (!File.Exists(sourceImagePath))
        {
            Console.WriteLine($"  [{logPrefix}] ⚠ Source image not found: {sourceImagePath}");
            return results;
        }

        // Check if smartcroppy is available (with auto-install)
        if (!await EnsureSmartcroppyAvailableAsync(logPrefix))
        {
            Console.WriteLine($"  [{logPrefix}] ⚠ smartcroppy not available - skipping image variations");
            return results;
        }

        // Get source image info
        string sourceDir = Path.GetDirectoryName(sourceImagePath) ?? ".";
        string sourceFileName = Path.GetFileNameWithoutExtension(sourceImagePath);
        string sourceExtension = Path.GetExtension(sourceImagePath);

        Console.WriteLine($"  [{logPrefix}] 🖼️  Generating {variations.Count} image variation(s)...");

        // Generate each variation
        foreach (ImageVariation variation in variations)
        {
            string outputFileName = $"{sourceFileName}_{variation.Width}_{variation.Height}{sourceExtension}";
            string outputPath = Path.Combine(sourceDir, outputFileName);

            try
            {
                bool success = await GenerateSingleVariationAsync(
                    sourceImagePath,
                    outputPath,
                    variation.Width,
                    variation.Height,
                    logPrefix);

                if (success)
                {
                    string key = $"{variation.Width}x{variation.Height}";
                    results[key] = outputPath;
                    
                    string desc = string.IsNullOrEmpty(variation.Description) 
                        ? "" 
                        : $" ({variation.Description})";
                    Console.WriteLine($"  [{logPrefix}] ✓ Generated {variation.Width}x{variation.Height}{desc}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [{logPrefix}] ✗ Failed {variation.Width}x{variation.Height}: {ex.Message}");
            }
        }

        if (results.Count > 0)
        {
            Console.WriteLine($"  [{logPrefix}] ✓ Generated {results.Count}/{variations.Count} variations");
        }

        return results;
    }

    /// <summary>
    /// Generates a single image variation using smartcroppy
    /// </summary>
    private static async Task<bool> GenerateSingleVariationAsync(
        string sourceImage,
        string outputImage,
        int width,
        int height,
        string logPrefix)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "smartcroppy",
            Arguments = $"--width {width} --height {height} \"{sourceImage}\" \"{outputImage}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine($"  [{logPrefix}] ✗ Failed to start smartcroppy process");
                return false;
            }

            // Read output asynchronously to prevent buffer deadlock
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"  [{logPrefix}] ✗ smartcroppy error: {error.Trim()}");
                }
                return false;
            }

            return File.Exists(outputImage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{logPrefix}] ✗ Exception running smartcroppy: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ensures smartcroppy is available, attempts to install if not present
    /// </summary>
    private static async Task<bool> EnsureSmartcroppyAvailableAsync(string logPrefix)
    {
        lock (_lockObject)
        {
            // Return cached result if we already checked
            if (_smartcroppyAvailable.HasValue)
                return _smartcroppyAvailable.Value;
        }

        // Check if smartcroppy is available
        if (await IsCommandAvailableAsync("smartcroppy"))
        {
            lock (_lockObject)
            {
                _smartcroppyAvailable = true;
            }
            return true;
        }

        Console.WriteLine($"  [{logPrefix}] 📦 smartcroppy not found, checking for pip...");

        // Check if pip is available
        if (!await IsPipAvailableAsync(logPrefix))
        {
            Console.WriteLine($"  [{logPrefix}] ⚠ pip not available - cannot install smartcroppy");
            Console.WriteLine($"  [{logPrefix}] ℹ️  Install Python and pip to enable image variations");
            lock (_lockObject)
            {
                _smartcroppyAvailable = false;
            }
            return false;
        }

        // Try to install smartcroppy
        Console.WriteLine($"  [{logPrefix}] 📦 Installing smartcroppy via pip...");
        if (await InstallSmartcroppyAsync(logPrefix))
        {
            Console.WriteLine($"  [{logPrefix}] ✓ smartcroppy installed successfully");
            lock (_lockObject)
            {
                _smartcroppyAvailable = true;
            }
            return true;
        }

        Console.WriteLine($"  [{logPrefix}] ✗ Failed to install smartcroppy");
        lock (_lockObject)
        {
            _smartcroppyAvailable = false;
        }
        return false;
    }

    /// <summary>
    /// Checks if pip is available
    /// </summary>
    private static async Task<bool> IsPipAvailableAsync(string logPrefix)
    {
        lock (_lockObject)
        {
            if (_pipAvailable.HasValue)
                return _pipAvailable.Value;
        }

        bool pipAvailable = await IsCommandAvailableAsync("pip") || 
                            await IsCommandAvailableAsync("pip3");

        lock (_lockObject)
        {
            _pipAvailable = pipAvailable;
        }

        return pipAvailable;
    }

    /// <summary>
    /// Attempts to install smartcroppy using pip
    /// </summary>
    private static async Task<bool> InstallSmartcroppyAsync(string logPrefix)
    {
        // Try pip first, then pip3
        string pipCommand = await IsCommandAvailableAsync("pip") ? "pip" : "pip3";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pipCommand,
            Arguments = "install smartcrop",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
                return false;

            // Show installation progress
            Task outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    string? line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Console.WriteLine($"  [{logPrefix}]   {line}");
                    }
                }
            });

            await process.WaitForExitAsync();
            await outputTask;

            if (process.ExitCode == 0)
            {
                // Verify installation by checking if smartcroppy is now available
                return await IsCommandAvailableAsync("smartcroppy");
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{logPrefix}] ✗ Error installing smartcroppy: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a command is available in the system PATH
    /// </summary>
    private static async Task<bool> IsCommandAvailableAsync(string command)
    {
        string whereCommand = OperatingSystem.IsWindows() ? "where" : "which";
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = whereCommand,
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
                return false;

            // Read output asynchronously to prevent buffer deadlock
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            
            // Ensure streams are fully read
            await outputTask;
            await errorTask;

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses image variation configuration from a list of target sizes
    /// </summary>
    public static List<ImageVariation> ParseVariations(params (int width, int height, string? description)[] sizes)
    {
        return sizes.Select(s => new ImageVariation
        {
            Width = s.width,
            Height = s.height,
            Description = s.description
        }).ToList();
    }
}

