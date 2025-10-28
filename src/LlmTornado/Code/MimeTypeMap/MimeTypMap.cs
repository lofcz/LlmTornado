using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace LlmTornado.Code.MimeTypeMap;

/// <summary>
/// Mime type map.
/// </summary>
internal static class MimeTypeMap
{
    private const string Dot = ".";
    private const string QuestionMark = "?";
    private const string DefaultMimeType = "application/octet-stream";
    private static readonly Lazy<IDictionary<string, string>> mappings = new Lazy<IDictionary<string, string>>(BuildMappings);

    private static Dictionary<string, string> BuildMappings()
    {
        Dictionary<string, string> localMappings = MimeTypeMapMapping.Mappings;

        List<KeyValuePair<string, string>> cache = localMappings.ToList();

        foreach (KeyValuePair<string, string> mapping in cache)
        {
            if (!localMappings.ContainsKey(mapping.Value))
            {
                localMappings.Add(mapping.Value, mapping.Key);
            }
        }

        return localMappings;
    }

    /// <summary>
    /// Tries to get the type of the MIME from the provided string (filename or extension).
    /// This method relies solely on the file extension and does not read file content.
    /// </summary>
    /// <param name="str">The filename or extension (e.g., "document.pdf" or "pdf").</param>
    /// <param name="mimeType">The variable to store the MIME type.</param>
    /// <returns>True if a MIME type was found for the extension, false otherwise.</returns>
    /// <exception cref="ArgumentNullException" />
    public static bool TryGetMimeType(string str, out string mimeType)
    {
        int indexQuestionMark = str.IndexOf(QuestionMark, StringComparison.Ordinal);

        if (indexQuestionMark != -1)
        {
            str = str.Remove(indexQuestionMark);
        }

        string extension = str;

        if (!str.StartsWith(Dot))
        {
            int index = str.LastIndexOf(Dot, StringComparison.Ordinal);

            if (index != -1 && str.Length > index + 1)
            {
#if NET8_0_OR_GREATER
                    extension = string.Concat(Dot, str.AsSpan(index + 1));
#else
                extension = Dot + str.Substring(index + 1);
#endif
            }
            else
            {
                extension = Dot + str;
            }
        }

        return mappings.Value.TryGetValue(extension, out mimeType);
    }

    /// <summary>
    /// Gets the type of the MIME from the provided string (filename or extension).
    /// This method relies solely on the file extension and does not read file content.
    /// </summary>
    /// <param name="str">The filename or extension.</param>
    /// <returns>The MIME type or "application/octet-stream" if not found.</returns>
    /// <exception cref="ArgumentNullException" />
    public static string GetMimeType(string str)
    {
        return TryGetMimeType(str, out string result) ? result : DefaultMimeType;
    }

    /// <summary>
    /// Tries to get the MIME type from a file stream, using magic bytes for more accurate detection and collision resolution.
    /// If magic bytes don't provide a definitive answer, it falls back to extension-based lookup.
    /// </summary>
    /// <param name="fileStream">The file stream. It will be read from its current position and then reset.</param>
    /// <param name="filename">Optional filename hint (e.g., "document.ts") to help resolve collisions, especially for ZIP-based formats or text files.</param>
    /// <param name="mimeType">The detected MIME type.</param>
    /// <returns>True if a MIME type was successfully determined, false otherwise.</returns>
    public static bool TryGetMimeType(string filename, Stream fileStream, out string mimeType)
    {
        mimeType = DefaultMimeType;

        if (!fileStream.CanRead || !fileStream.CanSeek)
        {
            return TryGetMimeType(filename, out mimeType);
        }

        long originalPosition = fileStream.Position;

        try
        {
            byte[] headerBytes = new byte[MagicByteDetector.MaxBytesToRead];
            int bytesRead = fileStream.Read(headerBytes, 0, headerBytes.Length);

            if (bytesRead == 0)
            {
                return TryGetMimeType(filename, out mimeType);
            }

            byte[] actualHeader = new byte[bytesRead];
            Buffer.BlockCopy(headerBytes, 0, actualHeader, 0, bytesRead);

            List<Info> magicByteMatches = MagicByteDetector.Detect(actualHeader);

            if (magicByteMatches.Count == 0)
            {
                return TryGetMimeType(filename, out mimeType);
            }

            string? preferredExtension = null;

            if (!string.IsNullOrEmpty(filename))
            {
                int lastDotIndex = filename.LastIndexOf('.');
                
                if (lastDotIndex != -1 && lastDotIndex < filename.Length - 1)
                {
                    preferredExtension = filename.Substring(lastDotIndex + 1).ToLowerInvariant();
                }
            }

            Info? bestMatch = null;

            if (!string.IsNullOrEmpty(preferredExtension))
            {
                bestMatch = magicByteMatches.FirstOrDefault(m => m.Extension != null && m.Extension.Equals(preferredExtension, StringComparison.OrdinalIgnoreCase));
            }

            switch (bestMatch)
            {
                case null when magicByteMatches.Count > 1:
                {
                    // PNG vs APNG (same magic bytes)
                    if (magicByteMatches.Any(m => m.TypeName == "png") && magicByteMatches.Any(m => m.TypeName == "apng"))
                    {
                        bestMatch = magicByteMatches.FirstOrDefault(m => m.TypeName == "apng" && preferredExtension == "apng") ?? magicByteMatches.First(m => m.TypeName == "png");
                    }

                    // ZIP-based formats (docx, xlsx, jar, odt, etc. all start with PK\x03\x04)
                    else if (magicByteMatches.Any(m => m.TypeName == "zip"))
                    {
                        bestMatch = magicByteMatches.FirstOrDefault(m => m.Extension != null && preferredExtension != null && m.Extension.Equals(preferredExtension, StringComparison.OrdinalIgnoreCase)) ?? magicByteMatches.FirstOrDefault(m => m.TypeName == "zip");
                    }

                    bestMatch ??= magicByteMatches.First();
                    break;
                }
                case null when magicByteMatches.Count == 1:
                {
                    bestMatch = magicByteMatches.First();
                    break;
                }
            }

            if (bestMatch?.Mime == null)
            {
                return TryGetMimeType(filename, out mimeType);
            }

            mimeType = bestMatch.Mime;
            return true;
        }
        finally
        {
            fileStream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Gets the MIME type from a file stream, using magic bytes for more accurate detection and collision resolution.
    /// </summary>
    /// <param name="fileStream">The file stream. It will be read from its current position and then reset.</param>
    /// <param name="mimeType">The detected MIME type.</param>
    /// <returns>True if a MIME type was successfully determined, false otherwise.</returns>
    public static bool TryGetMimeType(Stream fileStream, out string mimeType)
    {
        string fileName = string.Empty;
        
        #if NETSTANDARD2_0_OR_GREATER || NETFRAMEWORK || NETCOREAPP2_0_OR_GREATER
        if (fileStream is FileStream fs)
        {
            fileName = fs.Name;
        }
        #endif
        
        return TryGetMimeType(fileName, fileStream, out mimeType);
    }

    /// <summary>
    /// Gets the extension from the provided MIME type.
    /// </summary>
    /// <param name="mimeType">Type of the MIME.</param>
    /// <param name="extension">Extension of the file.</param>
    /// <returns>The extension.</returns>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="ArgumentException" />
    public static bool TryGetExtension(string mimeType, [NotNullWhen(true)] out string? extension)
    {
        return mimeType.StartsWith(Dot) ? throw new ArgumentException("Requested mime type is not valid: " + mimeType) : mappings.Value.TryGetValue(mimeType, out extension);
    }
}