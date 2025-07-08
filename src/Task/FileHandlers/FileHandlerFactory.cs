using System;
using System.Collections.Generic;

namespace AggregateConfigBuildTask.FileHandlers
{
    /// <summary>
    /// Factory class for creating appropriate file handlers and retrieving expected file extensions based on file type.
    /// </summary>
    public static class FileHandlerFactory
    {
        /// <summary>
        /// Returns the appropriate <see cref="IFileHandler"/> implementation based on the specified file format.
        /// </summary>
        /// <param name="fileSystem">The file system interface to be used by the handler.</param>
        /// <param name="format">The file type for which a handler is needed.</param>
        /// <returns>An instance of <see cref="IFileHandler"/> suitable for handling the specified format.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported file format is provided.</exception>
        internal static IFileHandler GetFileHandlerForType(IFileSystem fileSystem, FileType format)
        {
            switch (format)
            {
                case FileType.Json:
                    return new JsonFileHandler(fileSystem);
                case FileType.Yaml:
                    return new YamlFileHandler(fileSystem);
                case FileType.Arm:
                    return new ArmParametersFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported format", nameof(format));
            }
        }

        /// <summary>
        /// Retrieves a list of file extensions that are expected for the specified file type.
        /// </summary>
        /// <param name="inputType">The file type for which expected file extensions are requested.</param>
        /// <returns>A list of strings representing valid file extensions for the input type.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported file type is provided.</exception>
        internal static List<string> GetExpectedFileExtensions(FileType inputType)
        {
            switch (inputType)
            {
                case FileType.Json:
                    return new List<string> { ".json" };
                case FileType.Yaml:
                    return new List<string> { ".yml", ".yaml" };
                case FileType.Arm:
                    return new List<string> { ".json" };
                default:
                    throw new ArgumentException("Unsupported input type", nameof(inputType));
            }
        }
    }
}
