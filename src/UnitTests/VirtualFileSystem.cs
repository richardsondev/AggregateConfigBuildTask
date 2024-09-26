using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask.Tests.Unit
{
    internal sealed class VirtualFileSystem(bool isWindowsMode = true) : IFileSystem
    {
        private readonly bool isWindowsMode = isWindowsMode;
        private readonly ConcurrentDictionary<string, string> fileSystem = new(
            isWindowsMode ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private RegexOptions RegexOptions => isWindowsMode ? RegexOptions.IgnoreCase : RegexOptions.None;
        private StringComparison StringComparison => isWindowsMode ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        /// <inheritdoc/>
        public string[] GetFiles(string path, string searchPattern)
        {
            path = NormalizePath(path);

            var files = new List<string>();
            string regexPattern = ConvertPatternToRegex(searchPattern);

            foreach (var file in fileSystem.Keys)
            {
                // Ensure the file starts with the given path
                if (file.StartsWith(path, StringComparison))
                {
                    // Perform a regex match using the translated pattern
                    if (Regex.IsMatch(file, regexPattern, RegexOptions))
                    {
                        files.Add(file);
                    }
                }
            }

            return [.. files];
        }

        /// <inheritdoc/>
        public Task<string> ReadAllTextAsync(string path)
        {
            path = NormalizePath(path);

            if (fileSystem.TryGetValue(path, out var content))
            {
                return Task.FromResult(content);
            }

            throw new FileNotFoundException($"The file '{path}' was not found in the virtual file system.");
        }

        /// <inheritdoc/>
        public Task WriteAllTextAsync(string path, string text)
        {
            path = NormalizePath(path);
            string directoryPath = Path.GetDirectoryName(path);

            if (!DirectoryExists(directoryPath))
            {
                throw new IOException($"Directory not found '{directoryPath}'.");
            }

            // As a part of the testing, we want to ensure we only write once to each file.
            if (FileExists(path))
            {
                throw new IOException($"Tried to write to existing file '{path}'!");
            }

            fileSystem[path] = text;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool FileExists(string path)
        {
            path = NormalizePath(path);

            foreach (var file in fileSystem.Keys)
            {
                // Ensure the file starts with the given path
                if (file.Equals(path, StringComparison))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool DirectoryExists(string directoryPath)
        {
            directoryPath = NormalizePath(directoryPath);
            directoryPath = EnsureTrailingDirectorySeparator(directoryPath);

            foreach (var file in fileSystem.Keys)
            {
                if (file.Equals(directoryPath, StringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void CreateDirectory(string directoryPath)
        {
            directoryPath = NormalizePath(directoryPath);
            directoryPath = EnsureTrailingDirectorySeparator(directoryPath);

            fileSystem[directoryPath] = string.Empty;
        }

        /// <inheritdoc />
        public TextReader OpenText(string path)
        {
            return new StringReader(ReadAllTextAsync(path).GetAwaiter().GetResult());
        }

        /// <inheritdoc />
        public Stream OpenRead(string inputPath)
        {
            var byteArray = Encoding.UTF8.GetBytes(ReadAllTextAsync(inputPath).GetAwaiter().GetResult());
            return new MemoryStream(byteArray);
        }

        /// <summary>
        /// Ensures that the provided directory path ends with a directory separator character.
        /// </summary>
        /// <param name="directoryPath">The directory path to normalize.</param>
        private string EnsureTrailingDirectorySeparator(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return directoryPath;
            }

            char directorySeparator = isWindowsMode ? '\\' : '/';

            // Ensure the directory path ends with the correct directory separator
            if (!directoryPath.EndsWith(directorySeparator.ToString(), StringComparison.Ordinal))
            {
                directoryPath += directorySeparator;
            }

            return directoryPath;
        }

        /// <summary>
        /// Converts a file search pattern (with * and ?) to a regex pattern.
        /// </summary>
        /// <param name="searchPattern">The wild card path expression to convert to a regex pattern.</param>
        private static string ConvertPatternToRegex(string searchPattern)
        {
            // Escape special regex characters except for * and ?
            string escapedPattern = Regex.Escape(searchPattern);

            // Replace escaped * and ? with their regex equivalents
            escapedPattern = escapedPattern.Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal);

            // Add start and end anchors to ensure full-string match
            return "^" + escapedPattern + "$";
        }

        /// <summary>
        /// Normalizes file paths by replacing slashes with the correct directory separator for the platform.
        /// </summary>
        /// <param name="path">The file or directory path to normalize.</param>
        /// <returns>A normalized path string.</returns>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            char directorySeparator = isWindowsMode ? '\\' : '/';
            char alternativeSeparator = isWindowsMode ? '/' : '\\';

            // Normalize slashes to the correct directory separator
            string normalizedPath = path.Replace(alternativeSeparator, directorySeparator);

            // Trim any redundant trailing slashes except for root directory ("/" or "C:\\")
            if (normalizedPath.Length > 1 && normalizedPath.EndsWith(directorySeparator.ToString(), StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.TrimEnd(directorySeparator);
            }

            return normalizedPath;
        }
    }
}
