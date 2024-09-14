using System;
using System.Collections.Generic;

namespace AggregateConfigBuildTask.FileHandlers
{
    public static class FileHandlerFactory
    {
        internal static IOutputWriter GetOutputWriter(IFileSystem fileSystem, FileType format)
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
                    throw new ArgumentException("Unsupported format");
            }
        }

        internal static IInputReader GetInputReader(IFileSystem fileSystem, FileType format)
        {
            switch (format)
            {
                case FileType.Yaml:
                    return new YamlFileHandler(fileSystem);
                case FileType.Json:
                    return new JsonFileHandler(fileSystem);
                case FileType.Arm:
                    return new ArmParametersFileHandler(fileSystem);
                default:
                    throw new ArgumentException("Unsupported input format");
            }
        }

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
                    throw new ArgumentException("Unsupported input type");
            }
        }
    }
}
