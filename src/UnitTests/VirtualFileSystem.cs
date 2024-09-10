﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AggregateConfig.Tests.Unit
{
    internal class VirtualFileSystem : IFileSystem
    {
        private readonly bool isWindowsMode = false;
        private readonly Dictionary<string, string> fileSystem;

        private RegexOptions RegexOptions => isWindowsMode ? RegexOptions.IgnoreCase : RegexOptions.None;
        private StringComparison StringComparison => isWindowsMode ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private string EnvironmentLineBreak => isWindowsMode ? "\r\n" : "\n";

        public VirtualFileSystem(bool isWindowsMode = true)
        {
            this.isWindowsMode = isWindowsMode;
            this.fileSystem = new Dictionary<string, string>(
                isWindowsMode ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
            );
        }

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

            return files.ToArray();
        }

        /// <inheritdoc/>
        public string[] ReadAllLines(string path)
        {
            path = NormalizePath(path);

            var content = ReadAllText(path);
            return content.Split(EnvironmentLineBreak);
        }

        /// <inheritdoc/>
        public string ReadAllText(string path)
        {
            path = NormalizePath(path);

            if (fileSystem.TryGetValue(path, out var content))
            {
                return content;
            }

            throw new FileNotFoundException($"The file '{path}' was not found in the virtual file system.");
        }

        /// <inheritdoc/>
        public void WriteAllText(string path, string text)
        {
            path = NormalizePath(path);
            string directoryPath = Path.GetDirectoryName(path);

            if (!DirectoryExists(directoryPath))
            {
                throw new IOException($"Directory not found '{directoryPath}'.");
            }

            fileSystem[path] = text;
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
        public bool DirectoryExists(string path)
        {
            path = NormalizePath(path);
            path = EnsureTrailingDirectorySeparator(path);

            foreach (var file in fileSystem.Keys)
            {
                if (file.Equals(path, StringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void CreateDirectory(string path)
        {
            path = NormalizePath(path);
            path = EnsureTrailingDirectorySeparator(path);

            fileSystem[path] = string.Empty;
        }

        /// <inheritdoc />
        public TextReader OpenText(string path)
        {
            return new StringReader(ReadAllText(path));
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
            if (!directoryPath.EndsWith(directorySeparator.ToString()))
            {
                directoryPath += directorySeparator;
            }

            return directoryPath;
        }

        /// <summary>
        /// Converts a file search pattern (with * and ?) to a regex pattern.
        /// </summary>
        private string ConvertPatternToRegex(string searchPattern)
        {
            // Escape special regex characters except for * and ?
            string escapedPattern = Regex.Escape(searchPattern);

            // Replace escaped * and ? with their regex equivalents
            escapedPattern = escapedPattern.Replace("\\*", ".*").Replace("\\?", ".");

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
            if (normalizedPath.Length > 1 && normalizedPath.EndsWith(directorySeparator.ToString()))
            {
                normalizedPath = normalizedPath.TrimEnd(directorySeparator);
            }

            return normalizedPath;
        }
    }
}
